
#include <iostream>
#include <random>
#include <fstream>

using namespace std;



int main() {
    ifstream obj {"hge_sample_points_custom.txt"};
    ofstream obj_lr {"hge_sample_points.txt"};

    std::random_device dev;
    std::mt19937 rng(dev());
    std::uniform_real_distribution<float> dist(0.f, 1.f);

    float ratio = 0.03;

    while (true){
        int r, g, b;
        float x, y, z, nx, ny, nz;
        obj >> x >> y >> z >> r >> g >> b >> nx >> ny >> nz;
        if (obj.fail()) break;

        if (dist(rng) < ratio) obj_lr
            << x << " " << y << " " << z << " "
            << r << " " << g << " " <<  b << " "
            << nx << " " << ny << " " << nz << " ";
    }
    obj_lr.close();


}
