# NiNode Transform Tools

This is command line tools for easily porting transform parameters of NiNode.

## Prerequisites

- .NET Framework 4.5.2

## Usage

```
NiDumpScale.exe skeleton.nif
```
Selects NiNode (bone) that is not scale = 1.0 and outputs name and scale factor.

```
NiUpdateScale.exe skeleton.nif nodes.txt
```
Updates scale of NiNode (bone) and create out.nif.

Restriction: CME bone name conversion is not performed.

```
NiDumpCharGen.exe Preset.jslot
```
Dump transforms in CharGen Preset.

CharGen Presets folder:
Data\SKSE\Plugins\CharGen\Presets

```
NiTransform.exe skeleton.nif transforms.txt
```
Bake transforms into skeleton.nif and create out.nif.


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


## Concrete example - Bake CharGen Preset transforms into a skeleton

Execute the procedure from the command line as follows:

```
NiDumpCharGen.exe Preset.jslot > transforms.txt
NiTransform.exe skeleton.nif transforms.txt
rename out.nif skeleton_female.nif
```


## Format

### The format of NiDumpScale.exe output (tab delimited):

name scale

### ex. Vanilla skeleton_female.nif
```
NPC R Hand [RHnd]       0.851680
NPC L Hand [LHnd]       0.851680
```

### The format of NiDumpCharGen.exe output (tab delimited):

name method value

### ex. Breast slider
```
NPC R Breast    Scale   1.8
NPC L Breast    Scale   1.8
NPC R Breast01  Scale   0.5555556
NPC L Breast01  Scale   0.5555556
```

### ex. Breast Sagness/Cleavage slider
```
NPC R PreBreast Rotation        0.8660254 -0.5 0 0.5 0.8660254 0 0 0 1
NPC L PreBreast Rotation        1 0 0 0 0.8660254 0.5 0 -0.5 0.8660254
```

### ex. Head Up/Down slider
```
CME Neck [Neck] Position        0 0 -0.5
CME Camera1st [Cam1]    Position        0 0 -0.5
CME Camera3rd [Cam3]    Position        0 0 -0.5
```


## ChangeLog
v0.0.2
- Add NiDumpCharGen.exe
- Add NiTransform.exe

--
opparco
