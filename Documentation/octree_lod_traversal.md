**one node will always have the same parent, check is not needed.**

```
get currentNode

for each diagonally neighbouring node do:

    if neigbour has same parent node as currentNode
        all current node siblings should have lod = 0
    else
        neighbouring node siblings should have lod = 0

    save currentNode's parent and neighboring node's parent to list unless they already exists in list

for each parent in list do:
    get all siblings

    for each sibling do:
        if sibling does not equal parent
            sibling should have lod = 1

    save parent's parent to list and continue recursively until root node / desired depth






```
