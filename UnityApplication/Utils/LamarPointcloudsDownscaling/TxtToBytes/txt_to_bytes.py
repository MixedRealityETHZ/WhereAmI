import struct
from glob import glob


for path in glob("*.txt"):
    fname = path.split("/")[-1].split(".")[0]
    
    # vertices = []
    # colors = []
    # normals = []
    joint_data = []

    data = open(path, "r").read().split(" ")
    for i in range(0, (len(data)-8), 9):
        x, y, z = float(data[i]), float(data[i+1]), float(data[i+2]) 
        r, g, b = int(data[i+3]), int(data[i+4]), int(data[i+5])
        nx, ny, nz = float(data[i+6]), float(data[i+7]), float(data[i+8])
        # vertices.extend([x, y, z])
        # colors.extend([r, g, b])
        # normals.extend([nx, ny, nz])
        joint_data.extend([x, y, z, r, g, b, nx, ny, nz])

    with open(f"{fname}.bytes", "wb") as f:
        vert_count = len(vertices)//3
        f.write(struct.pack("I", vert_count))
        f.write(struct.pack("=" + "fffBBBfff"*vert_count, *joint_data))






