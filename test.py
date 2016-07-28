import numpy as np
from mpl_toolkits.mplot3d import Axes3D
import matplotlib.pyplot as plt

HDFace_fixed = "HDFace_original.txt"
HDFace_obj = "HDFace_moved.txt"

fig = plt.figure()
ax = fig.add_subplot(111, projection='3d')

mask_fixed = [tuple(map(float, line.split(' '))) for line in
              open(HDFace_fixed, "r")]
mask_obj = [tuple(map(float, line.split(' '))) for line in
              open(HDFace_obj, "r")]

for (x, y, z) in mask_fixed:
    ax.scatter(x, y, z, c="g")

for (x, y, z) in mask_obj:
    ax.scatter(x, y, z, c="r")

ax.set_xlabel('X Label')
ax.set_ylabel('Y Label')
ax.set_zlabel('Z Label')

plt.show()
