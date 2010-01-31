mkdir Bin
gmcs -lib:Bin -target:library -out:/Bin/Mosa.ClassLib.dll ClassLib/*.cs -nowarn:0414,0103,0219,0169,0162,0168
gmcs -lib:Bin -target:library -out:Bin/Mosa.DeviceSystem.dll -d:MONO -reference:Mosa.ClassLib.dll DeviceSystem/*.cs DeviceSystem/PCI/*.cs -nowarn:0414,0103,0219,0169,0162,0168
gmcs -lib:Bin -target:library -out:Bin/Mosa.DeviceDrivers.dll -reference:Mosa.ClassLib.dll -reference:Mosa.DeviceSystem.dll DeviceDrivers/*.cs DeviceDrivers/ISA/*.cs DeviceDrivers/PCI/*.cs DeviceDrivers/ScanCodeMap/*.cs -nowarn:0414,0103,0219,0169,0162,0168
gmcs -lib:Bin -target:library -out:Bin/Mosa.FileSystem.dll -reference:Mosa.ClassLib.dll -reference:Mosa.DeviceSystem.dll FileSystem/*.cs FileSystem/FAT/*.cs FileSystem/VFS/*.cs FileSystem/FAT/Find/*.cs -nowarn:0414,0103,0219,0169,0162,0168
gmcs -lib:Bin -target:library -out:Bin/Mosa.EmulatedKernel.dll -reference:Mosa.ClassLib.dll -reference:Mosa.DeviceSystem.dll EmulatedKernel/*.cs -nowarn:0414,0103,0219,0169,0162,0168
gmcs -lib:Bin -target:library -out:Bin/Mosa.EmulatedDevices.dll -unsafe -reference:Mosa.ClassLib.dll -reference:Mosa.DeviceSystem.dll -reference:Mosa.EmulatedKernel.dll -reference:System.dll -reference:System.Drawing.dll -reference:System.Windows.Forms.dll EmulatedDevices/*.cs EmulatedDevices/Emulated/*.cs EmulatedDevices/Synthetic/*.cs -nowarn:0414,0103,0219,0169,0162,0168
gmcs -lib:Bin -target:library -out:Bin/Mosa.Pictor.dll Pictor/*.cs Pictor/Transform/*.cs Pictor/PixelFormat/*.cs Pictor/VertexSource/*.cs -nowarn:0414,0103,0219,0169,0162,0168 -unsafe
gmcs -lib:Bin -target:library -out:Bin/Mosa.Pictor.UI.dll Pictor.UI/*.cs Pictor.UI/Dialogs/*.cs -unsafe -reference:Mosa.Pictor.dll -nowarn:0414,0103,0219,0169,0162,0168
gmcs -lib:Bin -target:exe -out:Bin/Emulator.exe -reference:Mosa.Pictor.dll -reference:Mosa.Pictor.UI.dll -reference:System.Drawing -reference:System.Windows.Forms -reference:Mosa.ClassLib.dll -reference:Mosa.DeviceSystem.dll -reference:Mosa.DeviceDrivers.dll -reference:Mosa.EmulatedKernel.dll -reference:Mosa.FileSystem.dll -reference:Mosa.EmulatedDevices.dll Emulator/*.cs -nowarn:0414,0103,0219,0169,0162,0168 -unsafe
gmcs -lib:Bin -target:exe -out:Bin/Mosa.Tools.CreateBootImage.exe -reference:Mosa.ClassLib.dll -reference:Mosa.DeviceSystem.dll -reference:Mosa.DeviceDrivers.dll -reference:Mosa.FileSystem.dll -reference:Mosa.EmulatedDevices.dll Tools/CreateBootImage/*.cs -nowarn:0414,0103,0219,0169,0162,0168
gmcs -lib:Bin -target:library -unsafe -out:Bin/Mosa.Kernel.dll Kernel/*.cs Kernel/Memory/*.cs Kernel/Memory/X86/*.cs -nowarn:0414,0103,0219,0169,0162,0168
gmcs -lib:Bin -target:library -unsafe -out:Bin/Mosa.Runtime.dll -reference:System.dll -reference:System.Data.dll -reference:System.Xml.dll -reference:Mosa.Kernel.dll Runtime/*.cs Runtime/CompilerFramework/*.cs Runtime/CompilerFramework/IL/*.cs Runtime/CompilerFramework/IR/*.cs Runtime/Linker/*.cs Runtime/Linker/Elf32/*.cs Runtime/Linker/Elf32/Sections/*.cs Runtime/Linker/Elf64/*.cs Runtime/Linker/Elf64/Sections/*.cs Runtime/Linker/PE/*.cs Runtime/Metadata/*.cs Runtime/Metadata/Blobs/*.cs Runtime/Metadata/Signatures/*.cs Runtime/Metadata/Tables/*.cs Runtime/Metadata/Runtime/*.cs Runtime/Vm/*.cs Runtime/Loader/*.cs Runtime/Loader/PE/*.cs Runtime/System/*.cs Runtime/System/IO/*.cs -nowarn:0414,0103,0219,0169,0162,0168
gmcs -lib:Bin -target:library -unsafe -out:Bin/Mosa.Platforms.x86.dll -reference:System.dll -reference:Mosa.Runtime.dll Platforms/x86/*.cs Platforms/x86/Constraints/*.cs Platforms/x86/Instructions/*.cs Platforms/x86/Instructions/Intrinsics/*.cs -nowarn:0414,0103,0219,0169,0162,0168
gmcs -lib:Bin -target:exe -unsafe -out:Bin/mosacl.exe -reference:Mosa.Kernel.dll -reference:Mosa.Platforms.x86.dll -reference:Mosa.Runtime.dll -reference:System.Core.dll -reference:System.Data.dll -reference:System.dll -reference:System.Xml.dll Tools/Compiler/*.cs Tools/Compiler/Boot/*.cs Tools/Compiler/Linkers/*.cs Tools/Compiler/LinkTimeCodeGeneration/*.cs Tools/Compiler/Symbols/Pdb/*.cs Tools/Compiler/TypeInitializers/*.cs Tools/Compiler/Metadata/*.cs -nowarn:0414,0103,0219,0169,0162,0168
gmcs -lib:Bin -target:exe -unsafe -out:Bin/Mosa.HelloWorld.exe -reference:System.dll -reference:Mosa.Platforms.x86.dll HelloWorld/*.cs -nowarn:0414,0103,0219,0169,0162,0168
