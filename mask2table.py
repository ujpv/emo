#!/usr/bin/env python

import sys
import progressbar

SYNTAX_MSG = "Syntax error:\npython mask2table.py MODEL_FILENAME MASK_FILENAME RESULT_FILENAME MAX_DIST\n"

# MASK_FILENAME = "mask_granny_positions.txt"
# HEAD_FILENAME = "head_granny_positions.txt"
# RESU_FILENAME = 'corresponds.txt'

MAX_DIST = 0.07

mask = []
head = []

VERT_MASK_COUNT = 0

def get_dist(p1, p2):
    return ((p1[0] - p2[0]) ** 2 + (p1[1] - p2[1]) ** 2 + (p1[2] - p2[2]) ** 2) ** (0.5)

def get_min_max(model):
    xs = [x for (x, _, _) in model]
    ys = [y for (_, y, _) in model]
    zs = [z for (_, _, z) in model]
    msg = "X-Coordinate max {min_x}, max {max_x}\n" \
          "Y-Coordinate max {min_y}, max {max_y}\n" \
          "Z-Coordinate max {min_z}, max {max_z}".format(min_x=min(xs), max_x=max(xs),
                                                         min_y=min(ys), max_y=max(ys),
                                                         min_z=min(zs), max_z=max(zs))
    return msg

def get_nearest(p, model):
    min_dist = get_dist(p, model[0])
    min_num = 0
    for (i, v) in enumerate(model):
        d = get_dist(p, v)
        if d < min_dist:
            min_dist = d
            min_num = i
    if min_dist > MAX_DIST:
        return VERT_MASK_COUNT
    else:
        return min_num


###################################################################################

if len(sys.argv) != 5:
    print(SYNTAX_MSG)
    exit()
else:
    try:
        MAX_DIST = float(sys.argv[4])
    except BaseException:
        print(SYNTAX_MSG)
        exit()
    HEAD_FILENAME = sys.argv[1]
    MASK_FILENAME = sys.argv[2]
    RESU_FILENAME = sys.argv[3]

for line in open(MASK_FILENAME, "r"):
    mask.append(tuple(map(float, line.split(' '))))

for line in open(HEAD_FILENAME, "r"):
    head.append(tuple(map(float, line.split(' '))))

VERT_MASK_COUNT = len(mask)

print("Mask loaded. Mask size: {}".format(len(mask)))
print(get_min_max(mask))
print("Head loaded. Head size: {}".format(len(head)))
print(get_min_max(head))

result = []
bar = progressbar.ProgressBar(max_value=len(head),
                              widgets=[
                                  progressbar.Bar('=', '[', ']'),
                                  ' ',
                                  progressbar.Percentage()])

for p in head:
    bar.update(bar.value + 1)
    result.append(get_nearest(p, mask))

with open(RESU_FILENAME, mode='wt', encoding='utf-8') as myfile:
    myfile.write('\n'.join(str(x) for x in result))

print("\nWriten {} numbers.".format(len(result)))
