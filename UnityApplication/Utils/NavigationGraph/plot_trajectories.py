import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D
import numpy as np
from mpl_toolkits.mplot3d.art3d import Line3DCollection
from tqdm import tqdm
import random
import struct




class Vertex:
    def __init__(self, pos, trajectory_i):
        self.pos = pos
        self.trajectory_i = trajectory_i
        self.edges = []

    def add_edge(self, b):
        self.edges.append(b)
        
    def export(self, f):
        f.write(struct.pack("=fffI"+"I"* len(self.edges), self.pos[0], self.pos[2], self.pos[1], len(self.edges), *self.edges))


class Graph:
    def __init__(self):
        self.verts = []

    def add_vertex(self, pos, trajectory_i):
        self.verts.append(Vertex(pos, trajectory_i))

    def add_edge(self, a, b):
        self.verts[a].add_edge(b)
        self.verts[b].add_edge(a)

    @property
    def vertex_count(self): return len(self.verts)

    def get_line_idx(self):
        return np.array([(vi, ei) for vi, v in enumerate(self.verts) for ei in self.verts[vi].edges if vi < ei])

    def get_line_segments(self, segments_is):
        return np.array([v.pos for v in self.verts])[segments_is]
    
    def find_path(self, i, j):
        queue = set()
        queue.add(i)

        vert_data = {i:(0, None)}
        while len(queue):
            best = min(queue, key=lambda x: vert_data[x][0])
            queue.remove(best)
            bst_dist = vert_data[best][0]
            if best == j:
                q = []
                nxt = best
                while nxt is not None:
                    q.append(nxt)
                    nxt = vert_data[q[-1]][1]
                return q
                

            for nxt in self.verts[best].edges:
                dst = (bst_dist + np.linalg.norm(self.verts[best].pos - self.verts[nxt].pos), best)
                if nxt in vert_data:
                    vert_data[nxt] = min(vert_data[nxt], dst, key=lambda x:x[0])
                else:
                    vert_data[nxt] = dst
                    queue.add(nxt)
        return None
    
    def export(self, f):
        f.write(struct.pack("=I", self.vertex_count))
        for v in self.verts:
            v.export(f)



def load_trajectories():
    all_ts = np.load("trajectories.npy")

    all_trajectories = []
    t = []
    for i in range(len(all_ts)):
        if np.all(all_ts[i] == 0):
            last = False
            all_trajectories.append(t)
            t = []
        else:
            t.append(all_ts[i])
    
    return all_trajectories


def filter_trajectory(t, desired_dist, optimize_tangent):
    j = 0
    while True:
        j += 1
        t_new = [t[0]]
        for i in range(1, len(t)-1):
            t1, t2 = t[i+1] - t[i], t[i] - t[i-1]
            dist = np.linalg.norm(t1 + t2)
            if dist > desired_dist or np.dot(t1, t2) / np.linalg.norm(t1) / np.linalg.norm(t2) < optimize_tangent or i % 2 == j % 2:
                t_new.append(t[i])    
        t_new.append(t[-1])

        if len(t_new) == len(t):
            return t_new
        t = t_new
        



def filter_trajectories(trajectories, desired_dist, optimize_tangent):
    verts_before = sum(len(t) for t in trajectories)
    ts = [filter_trajectory(t, desired_dist, optimize_tangent) for t in trajectories]
    verts_after = sum(len(t) for t in ts)
    print (f"Filted trajectories, vertices {verts_before} -> {verts_after}")
    return ts


def create_graph(trajectories):
    trajectory_vert_indices = []
    g = Graph()
    for ti, t in enumerate(trajectories):        
        trajectory_vert_indices.append([])
        for i, p in enumerate(t):
            trajectory_vert_indices[-1].append(g.vertex_count)
            g.add_vertex(p, ti)
            if i != 0:
                g.add_edge(g.vertex_count-1, g.vertex_count-2)
        
    return g, trajectory_vert_indices



full_trajectories = load_trajectories()
filter_dist, filter_tangent = 20.0, 0.0
trajectories = filter_trajectories(full_trajectories, filter_dist, filter_tangent)
g, trajectory_vert_indices = create_graph(trajectories)



merge_dist = 5

positions = np.array([v.pos for v in g.verts], dtype=np.float32)

edges_added = 0
for vert_i, v in tqdm(enumerate(g.verts), total=g.vertex_count):
    for trajectory_i, t in enumerate(trajectories):
        if trajectory_i == v.trajectory_i: continue
        dists = np.linalg.norm(v.pos - t, axis=-1)
        min_dist_i = np.argmin(dists)
        if dists[min_dist_i] < merge_dist:
            g.add_edge(vert_i, trajectory_vert_indices[trajectory_i][min_dist_i])
            edges_added += 1
    
print (f"Edges added: {edges_added}")

def path_segments(path):
    return np.stack((path[1:], path[:-1]), 1)

with open("navigation_graph.bytes", "wb") as f:
    g.export(f)


segments = g.get_line_segments(g.get_line_idx())

path_from, path_to = random.randint(0, g.vertex_count-1), random.randint(0, g.vertex_count-1)
path_vertices = g.find_path(path_from, path_to)
path = g.get_line_segments(path_segments(path_vertices))



fig = plt.figure()
ax = fig.add_subplot(111, projection='3d')

line_collection = Line3DCollection(segments, linewidths=0.5, colors='blue', alpha=0.7)
path_collection = Line3DCollection(path + np.array([0, 0, 10.0]), linewidths=0.5, colors='green', alpha=0.7)


# Add the collection to the plot
ax.add_collection3d(line_collection)
ax.add_collection3d(path_collection)


ax.set_xlim(np.min(segments[..., 0]), np.max(segments[..., 0]))  # Z-axis range
ax.set_ylim(np.min(segments[..., 1]), np.max(segments[..., 1]))  # Z-axis range

ax.set_zlim(-50, 50)  # Z-axis range
# Optional: Set labels and title
ax.set_xlabel('X')
ax.set_ylabel('Y')
ax.set_zlabel('Z')
ax.set_title('3D Lines')

# Show the plot
plt.show()