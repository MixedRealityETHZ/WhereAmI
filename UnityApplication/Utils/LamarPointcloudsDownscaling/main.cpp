#include <openvdb/openvdb.h>
#include <iostream>
#include <string>

#include "load_ply.h"


using namespace std;




struct PointCloudFile{
    string filename;
    Transform transform;
};


vector<PointCloudFile> getDatasetFiles(const string& list_filename){
    ifstream f (list_filename);
    assert(f.is_open() && "The dataset file could not be opened");


    vector<PointCloudFile> fs;
    while (!f.eof()){
        string filename;
        float x, y, z, qx, qy, qz, qw;
        f >> filename >> x >> y >> z >> qw >> qx >> qy >> qz;
        if (f.fail()) break;

        fs.emplace_back(PointCloudFile{filename, Transform(x, y, z, qx, qy, qz, qw)});
    }
    return fs;
}



int main(int argc, char* argv[])
{
    assert (argc >= 2 && "Mode (--density/--points) must be specified");

    vector<PointCloudFile> files = getDatasetFiles("point_clouds.txt");

    enum Mode{
        SAMPLE_POINTS_CUSTOM,
        SAMPLE_POINTS_BLENDER,
        CONVERT_DENSITY,
        NONE
    };

    Mode mode = NONE;
    if (string(argv[1]) == "--points_custom") mode = SAMPLE_POINTS_CUSTOM;
    else if (string(argv[1]) == "--points_blender") mode = SAMPLE_POINTS_BLENDER;
    else if (string(argv[1]) == "--density") mode = CONVERT_DENSITY;
    else assert(false && "Invalid mode specified");


    if (mode == SAMPLE_POINTS_BLENDER || mode == SAMPLE_POINTS_CUSTOM){
        assert (argc >= 3 && "Point ratio must be specified as a third argument");

        int sample_ratio = std::stoi(string(argv[2]));
        ofstream output_file {mode == SAMPLE_POINTS_BLENDER ? "hge_sample_points.obj" : "hge_sample_points_custom.txt"};
        for (const PointCloudFile& file : files) {
            PointLoader points = load_ply_points(file.filename, file.transform);

            cout << "Sampling points... " << flush;

            if (mode == SAMPLE_POINTS_BLENDER){
                for (size_t i = 0; i < points.size(); i += sample_ratio){
                    Vector3 pos = points.get_position(i);
                    output_file << "v " << pos.x << ' ' << pos.y << ' ' << pos.z << '\n';
                }
            }else{
                for (size_t i = 0; i < points.size(); i += sample_ratio){
                    Vector3 pos = points.get_position(i);
                    Color color = points.get_color(i);
                    Vector3 normal = points.get_normal(i);
                    output_file << pos.x << ' ' << pos.y << ' ' << pos.z << ' '
                            << (int) color.r << ' ' << (int) color.g << ' ' << (int) color.b << ' '
                            << normal.x << ' ' << normal.y << ' ' << normal.z << '\n';
                }
            }
            cout << "Done." << endl;
        }
        output_file.close();
    } else {
        assert (argc >= 3 && "Voxels per unit length must be specified as a third argument");

        float voxels_per_unit = std::stof(string(argv[2]));

        openvdb::initialize();
        openvdb::FloatGrid::Ptr grid = openvdb::FloatGrid::create();;
        openvdb::FloatGrid::Accessor accessor = grid->getAccessor();

        for (const PointCloudFile& file : files) {
            PointLoader points = load_ply_points(file.filename, file.transform);

            cout << "Adding density points... " << flush;
            for (int i = 0; i < points.size(); i++){
                Vector3 point = points.get_position(i);
                openvdb::Coord xyz((int) (point.x * voxels_per_unit), (int) (point.y * voxels_per_unit), (int) (point.z * voxels_per_unit));
                accessor.setValue(xyz, 1 + accessor.getValue(xyz));
            }
            cout << "Done." << endl;
        }

        grid->setTransform(openvdb::math::Transform::createLinearTransform(1.f / voxels_per_unit));
        // Identify the grid as a level set.
        grid->setGridClass(openvdb::GRID_STAGGERED);

        grid->setName("HGE");
        // Create a VDB file object.
        openvdb::io::File file("hge_volume.vdb");
        // Add the grid pointer to a container.
        openvdb::GridPtrVec grids;
        grids.push_back(grid);
        // Write out the contents of the container.
        file.write(grids);
        file.close();
    }
}