// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Compiler.Common;
using Mosa.Compiler.Framework;
using Mosa.Compiler.MosaTypeSystem;

namespace Mosa.Platform.x64
{
	/// <summary>
	/// This class provides a common base class for architecture
	/// specific operations.
	/// </summary>
	public class Architecture : BaseArchitecture
	{
		/// <summary>
		/// Gets the endianness of the target architecture.
		/// </summary>
		/// <value>
		/// The endianness.
		/// </value>
		public override Endianness Endianness { get { return Endianness.Little; } }

		/// <summary>
		/// Gets the type of the elf machine.
		/// </summary>
		/// <value>
		/// The type of the elf machine.
		/// </value>
		public override ushort ElfMachineType { get { return 3; } }

		/// <summary>
		/// Defines the register set of the target architecture.
		/// </summary>
		private static readonly Register[] Registers = new Register[]
		{
			//TODO
		};

		/// <summary>
		/// Specifies the architecture features to use in generated code.
		/// </summary>
		private ArchitectureFeatureFlags architectureFeatures;

		/// <summary>
		/// Initializes a new instance of the <see cref="Architecture"/> class.
		/// </summary>
		/// <param name="architectureFeatures">The features this architecture supports.</param>
		private Architecture(ArchitectureFeatureFlags architectureFeatures)
		{
			this.architectureFeatures = architectureFeatures;
		}

		/// <summary>
		/// Retrieves the native integer size of the x64 platform.
		/// </summary>
		/// <value>This property always returns 64.</value>
		public override int NativeIntegerSize
		{
			get { return 64; }
		}

		/// <summary>
		/// Gets the native alignment of the architecture in bytes.
		/// </summary>
		/// <value>This property always returns 8.</value>
		public override int NativeAlignment
		{
			get { return 8; }
		}

		/// <summary>
		/// Gets the native size of architecture in bytes.
		/// </summary>
		/// <value>This property always returns 8.</value>
		public override int NativePointerSize
		{
			get { return 8; }
		}

		/// <summary>
		/// Retrieves the register set of the x64 platform.
		/// </summary>
		public override Register[] RegisterSet
		{
			get { return Registers; }
		}

		/// <summary>
		/// Retrieves the stack frame register of the x64.
		/// </summary>
		public override Register StackFrameRegister
		{
			get { return null; /* GeneralPurposeRegister.EBP;*/ }
		}

		/// <summary>
		/// Retrieves the stack pointer register of the x64.
		/// </summary>
		public override Register StackPointerRegister
		{
			get { return null; /* GeneralPurposeRegister.ESP;*/ }
		}

		/// <summary>
		/// Retrieves the exception register of the architecture.
		/// </summary>
		public override Register ExceptionRegister
		{
			get { return null; /* GeneralPurposeRegister.EDI;*/ }
		}

		/// <summary>
		/// Gets the finally return block register.
		/// </summary>
		public override Register LeaveTargetRegister
		{
			get { return null; /* GeneralPurposeRegister.EDX;*/ }
		}

		/// <summary>
		/// Retrieves the stack pointer register of the x64.
		/// </summary>
		public override Register ProgramCounter
		{
			get { return null; }
		}

		/// <summary>
		/// Gets the name of the platform.
		/// </summary>
		/// <value>
		/// The name of the platform.
		/// </value>
		public override string PlatformName { get { return "x64"; } }

		/// <summary>
		/// Factory method for the Architecture class.
		/// </summary>
		/// <returns>The created architecture instance.</returns>
		/// <param name="architectureFeatures">The features available in the architecture and code generation.</param>
		/// <remarks>
		/// This method creates an instance of an appropriate architecture class, which supports the specific
		/// architecture features.
		/// </remarks>
		public static BaseArchitecture CreateArchitecture(ArchitectureFeatureFlags architectureFeatures)
		{
			if (architectureFeatures == ArchitectureFeatureFlags.AutoDetect)
				architectureFeatures = ArchitectureFeatureFlags.MMX | ArchitectureFeatureFlags.SSE | ArchitectureFeatureFlags.SSE2;

			return new Architecture(architectureFeatures);
		}

		/// <summary>
		/// Extends the pre compiler pipeline with x64 specific stages.
		/// </summary>
		/// <param name="compilerPipeline">The pipeline to extend.</param>
		public override void ExtendPreCompilerPipeline(CompilerPipeline compilerPipeline)
		{
			//assemblyCompilerPipeline.InsertAfterFirst<ICompilerStage>(
			//    new InterruptVectorStage()
			//);

			//assemblyCompilerPipeline.InsertAfterFirst<InterruptVectorStage>(
			//    new ExceptionVectorStage()
			//);

			//assemblyCompilerPipeline.InsertAfterLast<TypeLayoutStage>(
			//    new MethodTableBuilderStage()
			//);
		}

		/// <summary>
		/// Extends the post compiler pipeline with x64 specific stages.
		/// </summary>
		/// <param name="compilerPipeline">The pipeline to extend.</param>
		public override void ExtendPostCompilerPipeline(CompilerPipeline compilerPipeline)
		{
		}

		/// <summary>
		/// Extends the method compiler pipeline with x64 specific stages.
		/// </summary>
		/// <param name="methodCompilerPipeline">The method compiler pipeline to extend.</param>
		public override void ExtendMethodCompilerPipeline(CompilerPipeline methodCompilerPipeline)
		{
			//methodCompilerPipeline.InsertAfterLast<PlatformStubStage>(
			//    new IMethodCompilerStage[]
			//    {
			//        new LongOperandTransformationStage(),
			//        new AddressModeConversionStage(),
			//        new IRTransformationStage(),
			//        new TweakTransformationStage(),
			//        new MemToMemConversionStage(),
			//    });

			//methodCompilerPipeline.InsertAfterLast<IBlockOrderStage>(
			//    new SimplePeepholeOptimizationStage()
			//);

			//methodCompilerPipeline.InsertAfterLast<CodeGenerationStage>(
			//    new ExceptionLayoutStage()
			//);
		}

		/// <summary>
		/// Gets the type memory requirements.
		/// </summary>
		/// <param name="typeLayout">The type layouts.</param>
		/// <param name="type">The signature type.</param>
		/// <param name="size">Receives the memory size of the type.</param>
		/// <param name="alignment">Receives alignment requirements of the type.</param>
		public override void GetTypeRequirements(MosaTypeLayout typeLayout, MosaType type, out int size, out int alignment)
		{
			if (type.IsUI8 || type.IsR8 || !type.IsValueType || type.IsPointer)
			{
				size = 8;
				alignment = 8;
			}
			else if (typeLayout.IsCompoundType(type))
			{
				size = typeLayout.GetTypeSize(type);
				alignment = 8;
			}
			else
			{
				size = 4;
				alignment = 4;
			}
		}

		/// <summary>
		/// Gets the code emitter.
		/// </summary>
		/// <returns></returns>
		public override BaseCodeEmitter GetCodeEmitter()
		{
			// TODO
			return null;

			//return new MachineCodeEmitter();
		}

		/// <summary>
		/// Inserts the move instruction.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="destination">The destination.</param>
		/// <param name="source">The source.</param>
		public override void InsertMoveInstruction(Context context, Operand destination, Operand source)
		{
			// TODO
		}

		/// <summary>
		/// Create platform compound move.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="destination">The destination.</param>
		/// <param name="source">The source.</param>
		/// <param name="size">The size.</param>
		public override void InsertCompoundMoveInstruction(BaseMethodCompiler compiler, Context context, Operand destination, Operand source, int size)
		{
			// TODO
		}

		/// <summary>
		/// Inserts the exchange instruction.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="destination">The destination.</param>
		/// <param name="source">The source.</param>
		public override void InsertExchangeInstruction(Context context, Operand destination, Operand source)
		{
			// TODO
		}

		/// <summary>
		/// Inserts the jump instruction.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="destination">The destination.</param>
		/// <param name="source">The source.</param>
		public override void InsertJumpInstruction(Context context, Operand destination)
		{
			// TODO
		}

		/// <summary>
		/// Inserts the jump instruction.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="Destination">The destination.</param>
		public override void InsertJumpInstruction(Context context, BasicBlock Destination)
		{
			// TODO
		}

		/// <summary>
		/// Inserts the call instruction.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="source">The source.</param>
		public override void InsertCallInstruction(Context context, Operand source)
		{
			// TODO
		}

		/// <summary>
		/// Inserts the add instruction.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="Destination">The destination.</param>
		/// <param name="Source">The source.</param>
		public override void InsertAddInstruction(Context context, Operand destination, Operand source1, Operand source2)
		{
			// TODO
		}

		/// <summary>
		/// Inserts the sub instruction.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="Destination">The destination.</param>
		/// <param name="Source">The source.</param>
		public override void InsertSubInstruction(Context context, Operand destination, Operand source1, Operand source2)
		{
			// TODO
		}

		/// <summary>
		/// Determines whether [is instruction move] [the specified instruction].
		/// </summary>
		/// <param name="instruction">The instruction.</param>
		/// <returns></returns>
		public override bool IsInstructionMove(BaseInstruction instruction)
		{
			// TODO
			return false;
		}

		/// <summary>
		/// Inserts the address of instruction.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="destination">The destination.</param>
		/// <param name="source">The source.</param>
		public override void InsertAddressOfInstruction(Context context, Operand destination, Operand source)
		{
			// TODO
		}
	}
}
