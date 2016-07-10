import progressbar

MASK_FILENAME = "mask_granny_positions.txt"
HEAD_FILENAME = "head_granny_positions.txt"
RESULT_FILE_NAME = 'corresponds.txt'

MAX_DIST = 0.07

mask = []
head = []

VERT_MASK_COUNT = 100500

def get_dist(p1, p2):
    return ((p1[0] - p2[0]) ** 2 + (p1[1] - p2[1]) ** 2 + (p1[2] - p2[2]) ** 2) ** (0.5)

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


for line in open(MASK_FILENAME, "r"):
    mask.append(tuple(map(float, line.split(' '))))

for line in open(HEAD_FILENAME, "r"):
    head.append(tuple(map(float, line.split(' '))))

VERT_MASK_COUNT = len(mask)

print("Mask loaded. Mask size: {}".format(len(mask)))
print("Head loaded. Head size: {}".format(len(head)))

result = []
bar = progressbar.ProgressBar(max_value=len(head),
                              widgets=[
                                  progressbar.Bar('=', '[', ']'),
                                  ' ',
                                  progressbar.Percentage()])
for p in head:
    bar.update(bar.value + 1)
    result.append(get_nearest(p, mask))

with open(RESULT_FILE_NAME, mode='wt', encoding='utf-8') as myfile:
    myfile.write('\n'.join(str(x) for x in result))

print("\nWriten {} numbers.".format(len(result)))

