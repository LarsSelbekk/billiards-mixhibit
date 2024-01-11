from math import sin, cos, pi, sqrt

r = 0.028575

for row_index, row_length in enumerate((1, 2, 3, 4, 5)):
    y = row_index * sqrt(3) * r
    start_x = - (row_length - 1) * r
    for i in range(row_length):
        print((start_x + r * 2 * i, y), end="")
        input()
