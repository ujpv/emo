#!/usr/bin/env python

import os

MASK_FILE_NAMES = {
    "vove/00_boy_10/mask.txt",
    "vove/01_boy_16/mask.txt",
    "vove/02_man_30/mask.txt",
    "vove/03_oldman/mask.txt",
    "vove/04_girl_10/mask.txt",
    "vove/05_girl_16/mask.txt",
    "vove/06_woman_30/mask.txt",
    "vove/07_granny/mask.txt"
}

HDFace_origin = "HDFace_original.txt"
HDFace_moved = "HDFace_moved.txt"

corresponds = []


def get_nearest(p, model):
    def get_dist(p1, p2):
        return ((p1[0] - p2[0]) ** 2 + (p1[1] - p2[1]) ** 2 + (p1[2] - p2[2]) ** 2) ** 0.5

    min_dist, min_num = get_dist(p, model[0]), 0
    for (i, v) in enumerate(model):
        d = get_dist(p, v)
        if d < min_dist:
            min_dist, min_num = d, i
    return min_num


def fix_mask(in_file_name, out_file_name):
    lines = [l for l in open(in_file_name, "r")]
    result = [str()] * len(lines)
    for (i, n) in enumerate(corresponds):
        result[i] = lines[n]
    with open(out_file_name, mode='wt', encoding='utf-8') as out_file:
        out_file.write(''.join(result))


def prepare():
    mask_origin = [tuple(map(float, line.split(' '))) for line in
                   open(HDFace_origin, "r")]  # = [(x, y, z), ..., (x, y, z)]
    mask_moved = [tuple(map(float, line.split(' '))) for line in
                  open(HDFace_moved, "r")]
    for p in mask_origin:
        corresponds.append(get_nearest(p, mask_moved))
    print("Preparing...\n"
          "Origin mask size = {}, Moved mask size = {}, Corresponds table size = {}\n".format(
        len(mask_origin),
        len(mask_moved),
        len(corresponds)))


########################################################################################################################

prepare()

for name in MASK_FILE_NAMES:
    print("Processing {}...".format(name), end="")
    res_file_name = str(os.path.splitext(name)[0]) + "_fixed.txt"
    fix_mask(name, res_file_name)
    print("Done\nWrite {}".format(res_file_name))
