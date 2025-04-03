#include <iostream>
#include <fstream>
#include <string>
#include <map>
#include <vector>
#include <cassert>

using namespace std;




static bool starts_with(const string& str, const string& prefix) {
    return str.compare(0, prefix.size(), prefix) == 0;
}


static void split3(const string& s, string& a, string& b, string& c) {
    size_t first_space = s.find(' ');
    size_t second_space = s.find(' ', first_space + 1);
    a = s.substr(0, first_space);
    b = s.substr(first_space + 1, second_space - first_space - 1);
    c = s.substr(second_space + 1);
}



enum class PLYPropertyType {
    FLOAT,
    INT,
    UCHAR,
    //LIST,
    UNKNOWN
};

static int property_type_size(PLYPropertyType t) {
    if (t == PLYPropertyType::FLOAT) {
        return 4;
    }
    else if (t == PLYPropertyType::INT) {
        return 4;
    }
    else if (t == PLYPropertyType::UCHAR) {
        return 1;
    }
    else {
        assert(false);
    }
}

static PLYPropertyType type_from_string(const string& type) {
    if (type == "float") return PLYPropertyType::FLOAT;
    if (type == "int") return PLYPropertyType::INT;
    if (type == "uchar") return PLYPropertyType::UCHAR;
    // if (type == "list") return PropertyType::LIST;
    assert(false);
    return PLYPropertyType::UNKNOWN;
}



struct PLYProperty {
    string name;
    PLYPropertyType type;

    //will be needed for loading meshes. We ignore these for now
    //PropertyType list_type_size = PropertyType::UNKNOWN;
    //PropertyType list_type_element = PropertyType::UNKNOWN;

    PLYProperty(const string& name_, PLYPropertyType type_) : name(name_), type(type_) {

    }
};



struct PLYElement {
    string name;
    size_t count;
    vector<PLYProperty> properties;

    vector<char> read_data;

    PLYElement(const string& name_, size_t count_) : name(name_), count{ count_ }
    {}

    [[nodiscard]] size_t unit_size() const {
        size_t size = 0;
        for (const PLYProperty& p : properties) {
            size += property_type_size(p.type);
        }
        return size;
    }

    [[nodiscard]] size_t total_size() const {
        
        return unit_size() * count;
    }

    [[nodiscard]] int offset(const string& property_name) const {
        int offset = 0;
        for (const auto & property : properties) {
            if (property.name == property_name) return offset;
            offset += property_type_size(property.type);
        }
        assert(false);
    }

    void read(ifstream& file) {
        read_data.resize(total_size());
        file.read((char*)&read_data[0], (streamsize) read_data.size());
    }
};


template<typename T>
static const T& find_by_name(const vector<T>& elements, const string& name) {
    for (const T& e : elements) {
        if (e.name == name) {
            return e;
        }
    }
    assert(false);
}


static void loadPLY(const string& filename, vector<PLYElement>& elements, bool verbose = false) {
    ifstream file(filename, ios::in | ios::binary);

    assert (file.is_open() && "Failed to open the file");

    PLYElement* current_element = nullptr;

    string line;
    string line_a, line_b, line_c;
    bool reading_header = true;
    while (reading_header) {
        getline(file, line);
        split3(line, line_a, line_b, line_c);

        if (line_a == "element") {
            elements.emplace_back(line_b, stoi(line_c));
            current_element = &elements.back();
            if (verbose) cout << "Found element " << current_element->name << endl;
        }

        if (line_a == "property") {
            if (current_element == nullptr) assert(false);
            else {
                PLYPropertyType prop_type = type_from_string(line_b);
                current_element->properties.emplace_back(line_c, prop_type);
                if (verbose) cout << "Found property " << line_c << " for element " << current_element->name << endl;
            }
        }
        if (line == "end_header") {
            reading_header = false;
        }
    }

    for (PLYElement& e : elements) {
        e.read(file);
    }

    file.close();
}


static float read_float(const char* ptr) {
    float x;
    x = *((float*)ptr);
    return x;
}

static unsigned char read_unsigned_char(const char* ptr){
    unsigned char x;
    x = *((unsigned char*)ptr);
    return x;
}



class Quaternion{
public:
    float x, y, z, w;
    Quaternion(float w_, float x_, float y_, float z_){
        x = x_;
        y = y_;
        z = z_;
        w = w_;
    }

    [[nodiscard]] Quaternion Hamilton(const Quaternion& b) const{
        return {
                w * b.w - x * b.x - y * b.y - z * b.z,
                w * b.x + x * b.w + y * b.z - z * b.y,
                w * b.y - x * b.z + y * b.w + z * b.x,
                w * b.z + x * b.y - y * b.x + z * b.w
        };
    }
    [[nodiscard]] Quaternion Inverse() const{
        return {w, -x, -y, -z};
    }
};


class Transform{
public:
    float tx, ty, tz;
    Quaternion q;

    Transform(float tx, float ty, float tz, float qx, float qy, float qz, float qw) :
        tx(tx), ty(ty), tz(tz), q(qw, qx, qy, qz){}

    inline void apply(float x, float y, float z, float& ox, float& oy, float& oz) const{
        Quaternion after_rot = q.Hamilton(Quaternion(0, x, y, z)).Hamilton(q.Inverse());
        ox = after_rot.x + tx;
        oy = after_rot.y + ty;
        oz = after_rot.z + tz;
    }
    inline void apply_rotation(float x, float y, float z, float& ox, float& oy, float& oz) const {
        Quaternion after_rot = q.Hamilton(Quaternion(0, x, y, z)).Hamilton(q.Inverse());
        ox = after_rot.x;
        oy = after_rot.y;
        oz = after_rot.z;
    }

};

struct Vector3{
    float x, y, z;
};

struct Color{
    unsigned char r, g, b;
};


class PointLoader{
    vector<PLYElement> elements;

    const PLYElement& vertices;
    size_t data_stride;
    int x, y, z, r, g, b, nx, ny, nz;
    const Transform& transform;
public:
    PointLoader(vector<PLYElement> elements_, const Transform& transform) :
        elements(std::move(elements_)),
        vertices(find_by_name(elements, "vertex")), data_stride(vertices.unit_size()),
        x(vertices.offset("x")), y(vertices.offset("y")), z(vertices.offset("z")),
        r(vertices.offset("red")), g(vertices.offset("green")), b(vertices.offset("blue")),
        nx(vertices.offset("nx")), ny(vertices.offset("ny")), nz(vertices.offset("nz")),
        transform(transform)
    {}

    [[nodiscard]] Vector3 get_position(size_t i) const {
        size_t data_pos = data_stride * i;
        float lx = read_float(&vertices.read_data[data_pos + x]);
        float ly = read_float(&vertices.read_data[data_pos + y]);
        float lz = read_float(&vertices.read_data[data_pos + z]);
        float gx, gy, gz;
        transform.apply(lx, ly, lz, gx, gy, gz);
        return {gx, gz, gy};
    }
    [[nodiscard]] Color get_color(size_t i) const {
        size_t data_pos = data_stride * i;
        return {
            read_unsigned_char(&vertices.read_data[data_pos + r]),
            read_unsigned_char(&vertices.read_data[data_pos + g]),
            read_unsigned_char(&vertices.read_data[data_pos + b])
        };
    }
    [[nodiscard]] Vector3 get_normal(size_t i) const{
        size_t data_pos = data_stride * i;
        float lx = read_float(&vertices.read_data[data_pos + nx]);
        float ly = read_float(&vertices.read_data[data_pos + ny]);
        float lz = read_float(&vertices.read_data[data_pos + nz]);
        float gx, gy, gz;
        transform.apply_rotation(lx, ly, lz, gx, gy, gz);

        return {gx, gz, gy};
    }

    [[nodiscard]] size_t size() const{
        return vertices.count;
    }
};



static PointLoader load_ply_points(const string& input_filename, const Transform& transform){
    cout << "Loading file " << input_filename << "... " << flush;
    vector<PLYElement> elements;
    loadPLY(input_filename, elements);
    cout << "Done." << endl;
    return {std::move(elements), transform};
}
