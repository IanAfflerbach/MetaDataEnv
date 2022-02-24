import csv

filename = "../Assets/StreamingAssets/cities.csv"
newDataset = []
with open(filename) as file:
    reader = csv.reader(file)
    for row in reader:
        newRow = []
        for x in row:
            x = x.strip(' ')
            x = x.strip('"')
            newRow.append(x)
        newDataset.append(newRow)

with open(filename, "w") as file:
    writer = csv.writer(file)
    for row in newDataset:
        writer.writerow(row)