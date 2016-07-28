#!/usr/bin/env python


def get_not_unique(ps):
    seen = set()
    not_unique = []
    for p in ps:
        if p in seen:
            not_unique.append(p)
            continue
        seen.add(p)
    return not_unique


ACCURACY = 1050

ORIGIN_FILENAME = "HDFace_original.txt"
MOVED_FILENAME = "HDFace_moved.txt"
# MOVED_FILENAME = "new_model.obj"

# points_origin = [line for line in open(ORIGIN_FILENAME, "r")]
points_origin = [tuple(map(lambda x: int(round(float(x) * ACCURACY)),
                           line.split(' ')))
                 for line in open(ORIGIN_FILENAME, "r")]
table_origin = {key: value for (value, key) in enumerate(points_origin)}

# points_moved = [line for line in open(MOVED_FILENAME, "r")]
points_moved = [tuple(map(lambda x: int(round(float(x) * ACCURACY)),
                          line.split(' ')))
                for line in open(MOVED_FILENAME, "r")]

print("Origin unique points count = {}, Moves unique points count = {}\n".format(len(table_origin),
                                                                                 len(set(points_moved))))
print("Not unique Origin points count", get_not_unique(points_origin), "\n")
print("Not unique Moved points count", get_not_unique(points_moved), "\n")

hit = []
mis = []

for p in points_moved:
    if p in table_origin:
        hit.append(p)
    else:
        mis.append(p)

print("hit = {}, miss = {}, total = {}\n".format(len(hit), len(mis), (len(hit) + len(mis))))

print("Unused points:\n{}\n".format("\n".join(str(p) for p in set(table_origin.keys()).difference(hit))))

print("Can't find: \n{}\n".format("\n".join(str(p) for p in mis)))
