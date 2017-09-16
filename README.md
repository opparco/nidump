# NiNode Scale Tools

a command line tool for easily porting the scale factor of NiNode.

## How to use

```
NiDumpScale.exe skeleton.nif
```
Selects NiNode (bone) that is not scale = 1.0 and outputs name and scale factor.

```
NiUpdateScale.exe skeleton.nif nodes.txt
```
Updates scale of NiNode (bone) and create out.nif.

The format of nodes.txt (tab delimited):
```
name scale
```

## Concrete example - Porting a skeleton based on Vanilla or XPMS to XPMSE

Prepare a skeleton with the following file name.

src.nif
: Skeleton based on Vanilla or XPMS (conversion source)

xpmse.nif
: XPMSE (conversion destination)

Execute the procedure from the command line as follows:
```
NiDumpScale.exe src.nif > nodes.txt
NiUpdateScale.exe xpmse.nif nodes.txt
rename out.nif skeleton_female.nif
```
Restriction: CME bone name conversion is not performed.
