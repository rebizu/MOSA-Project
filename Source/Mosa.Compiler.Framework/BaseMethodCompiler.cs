// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Compiler.Framework.Analysis;
using Mosa.Compiler.Framework.Stages;
using Mosa.Compiler.Linker;
using Mosa.Compiler.MosaTypeSystem;
using Mosa.Compiler.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mosa.Compiler.Framework
{
	/// <summary>
	/// Base class of a method compiler.
	/// </summary>
	/// <remarks>
	/// A method compiler is responsible for compiling a single function
	/// of an object. There are various classes derived from BaseMethodCompiler,
	/// which provide specific features, such as jit compilation, runtime
	/// optimized jitting and others. BaseMethodCompiler instances are usually
	/// created by invoking CreateMethodCompiler on a specific compiler
	/// instance.
	/// </remarks>
	public class BaseMethodCompiler
	{
		#region Data Members

		/// <summary>
		/// Holds the type initializer scheduler stage
		/// </summary>
		private TypeInitializerSchedulerStage typeInitializer;

		/// <summary>
		/// The empty operand list
		/// </summary>
		private static readonly Operand[] emptyOperandList = new Operand[0];

		/// <summary>
		/// Holds flag that will stop method compiler
		/// </summary>
		private bool stop;

		#endregion Data Members

		#region Properties

		/// <summary>
		/// Gets the Architecture to compile for.
		/// </summary>
		public BaseArchitecture Architecture { get; private set; }

		/// <summary>
		/// Gets the linker used to resolve external symbols.
		/// </summary>
		public BaseLinker Linker { get; private set; }

		/// <summary>
		/// Gets the method implementation being compiled.
		/// </summary>
		public MosaMethod Method { get; private set; }

		/// <summary>
		/// Gets the owner type of the method.
		/// </summary>
		public MosaType Type { get; private set; }

		/// <summary>
		/// Gets the basic blocks.
		/// </summary>
		/// <value>The basic blocks.</value>
		public BasicBlocks BasicBlocks { get; private set; }

		/// <summary>
		/// Retrieves the compilation scheduler.
		/// </summary>
		/// <value>The compilation scheduler.</value>
		public CompilationScheduler Scheduler { get; private set; }

		/// <summary>
		/// Provides access to the pipeline of this compiler.
		/// </summary>
		public CompilerPipeline Pipeline { get; private set; }

		/// <summary>
		/// Gets the type system.
		/// </summary>
		/// <value>The type system.</value>
		public TypeSystem TypeSystem { get; private set; }

		/// <summary>
		/// Gets the type layout.
		/// </summary>
		/// <value>The type layout.</value>
		public MosaTypeLayout TypeLayout { get; private set; }

		/// <summary>
		/// Gets the compiler trace handle
		/// </summary>
		/// <value>The log.</value>
		public CompilerTrace Trace { get; private set; }

		/// <summary>
		/// Gets the local variables.
		/// </summary>
		public Operand[] LocalVariables { get; private set; }

		/// <summary>
		/// Gets the assembly compiler.
		/// </summary>
		public BaseCompiler Compiler { get; private set; }

		/// <summary>
		/// Gets the stack layout.
		/// </summary>
		public StackLayout StackLayout { get; private set; }

		/// <summary>
		/// Gets the virtual register layout.
		/// </summary>
		public VirtualRegisters VirtualRegisters { get; private set; }

		/// <summary>
		/// Gets the dominance analysis.
		/// </summary>
		public Dominance DominanceAnalysis { get; private set; }

		/// <summary>
		/// Gets the parameters.
		/// </summary>
		public Operand[] Parameters { get { return StackLayout.Parameters; } }

		/// <summary>
		/// Gets the protected regions.
		/// </summary>
		/// <value>
		/// The protected regions.
		/// </value>
		public IList<ProtectedRegion> ProtectedRegions { get; private set; }

		/// <summary>
		/// Gets a value indicating whether [plugged method].
		/// </summary>
		public MosaMethod PluggedMethod { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this method is plugged.
		/// </summary>
		public bool IsPlugged { get { return PluggedMethod != null; } }

		/// <summary>
		/// Gets the thread identifier.
		/// </summary>
		/// <value>
		/// The thread identifier.
		/// </value>
		public int ThreadID { get; private set; }

		/// <summary>
		/// Gets the method data.
		/// </summary>
		/// <value>
		/// The method data.
		/// </value>
		public CompilerMethodData MethodData { get; private set; }

		#endregion Properties

		#region Construction

		/// <summary>
		/// Initializes a new instance of the <see cref="BaseMethodCompiler" /> class.
		/// </summary>
		/// <param name="compiler">The assembly compiler.</param>
		/// <param name="method">The method to compile by this instance.</param>
		/// <param name="basicBlocks">The basic blocks.</param>
		/// <param name="threadID">The thread identifier.</param>
		protected BaseMethodCompiler(BaseCompiler compiler, MosaMethod method, BasicBlocks basicBlocks, int threadID)
		{
			Compiler = compiler;
			Method = method;
			Type = method.DeclaringType;
			Scheduler = compiler.CompilationScheduler;
			Architecture = compiler.Architecture;
			TypeSystem = compiler.TypeSystem;
			TypeLayout = compiler.TypeLayout;
			Trace = compiler.CompilerTrace;
			Linker = compiler.Linker;
			BasicBlocks = basicBlocks ?? new BasicBlocks();
			Pipeline = new CompilerPipeline();
			StackLayout = new StackLayout(Architecture, method.Signature.Parameters.Count + (method.HasThis || method.HasExplicitThis ? 1 : 0));
			VirtualRegisters = new VirtualRegisters(Architecture);
			LocalVariables = emptyOperandList;
			ThreadID = threadID;
			DominanceAnalysis = new Dominance(Compiler.CompilerOptions.DominanceAnalysisFactory, BasicBlocks);
			PluggedMethod = compiler.PlugSystem.GetPlugMethod(Method);
			stop = false;

			MethodData = compiler.CompilerData.GetCompilerMethodData(Method);
			MethodData.Counters.Clear();

			EvaluateParameterOperands();
		}

		#endregion Construction

		#region Methods

		/// <summary>
		/// Evaluates the parameter operands.
		/// </summary>
		protected void EvaluateParameterOperands()
		{
			int index = 0;

			// Note: displacement is recalculated later
			int displacement = 4;

			if (Method.HasThis || Method.HasExplicitThis)
			{
				if (Type.IsValueType)
					StackLayout.SetStackParameter(index++, Type.ToManagedPointer(), displacement, "this");
				else
					StackLayout.SetStackParameter(index++, Type, displacement, "this");
			}

			foreach (var parameter in Method.Signature.Parameters)
			{
				StackLayout.SetStackParameter(index++, parameter.ParameterType, displacement, parameter.Name);
			}
		}

		/// <summary>
		/// Compiles the method referenced by this method compiler.
		/// </summary>
		public void Compile()
		{
			BeginCompile();

			foreach (IMethodCompilerStage stage in Pipeline)
			{
				//try
				{
					stage.Initialize(this);
					stage.Execute();

					Mosa.Compiler.Trace.InstructionLogger.Run(this, stage);

					if (stop)
						break;
				}

				//catch (Exception e)
				//{
				//	//	Trace.TraceListener.SubmitDebugStageInformation(Method, stage.Name + "-Exception", e.ToString());
				//	Trace.TraceListener.OnNewCompilerTraceEvent(CompilerEvent.Exception, Method.FullName + " @ " + stage.Name, ThreadID);
				//	return;
				//}
			}

			InitializeType();

			var log = new TraceLog(TraceType.Counters, this.Method, string.Empty, Trace.TraceFilter.Active);
			log.Log(MethodData.Counters.Export());
			Trace.TraceListener.OnNewTraceLog(log);

			EndCompile();
		}

		/// <summary>
		/// Stops the method compiler.
		/// </summary>
		/// <returns></returns>
		public void Stop()
		{
			stop = true;
		}

		/// <summary>
		/// Creates a new virtual register operand.
		/// </summary>
		/// <param name="type">The signature type of the virtual register.</param>
		/// <returns>
		/// An operand, which represents the virtual register.
		/// </returns>
		public Operand CreateVirtualRegister(MosaType type)
		{
			return VirtualRegisters.Allocate(type);
		}

		/// <summary>
		/// Retrieves the local stack operand at the specified <paramref name="index" />.
		/// </summary>
		/// <param name="index">The index of the stack operand to retrieve.</param>
		/// <returns>
		/// The operand at the specified index.
		/// </returns>
		/// <exception cref="System.ArgumentOutOfRangeException">The <paramref name="index" /> is not valid.</exception>
		public Operand GetLocalOperand(int index)
		{
			return LocalVariables[index];
		}

		/// <summary>
		/// Retrieves the parameter operand at the specified <paramref name="index"/>.
		/// </summary>
		/// <param name="index">The index of the parameter operand to retrieve.</param>
		/// <returns>The operand at the specified index.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">The <paramref name="index"/> is not valid.</exception>
		public Operand GetParameterOperand(int index)
		{
			return StackLayout.Parameters[index];
		}

		/// <summary>
		/// Allocates the local variable virtual registers.
		/// </summary>
		/// <param name="locals">The locals.</param>
		public void SetLocalVariables(IList<MosaLocal> locals)
		{
			LocalVariables = new Operand[locals.Count];

			int index = 0;
			foreach (var local in locals)
			{
				var operand = StackLayout.AddStackLocal(local.Type, local.IsPinned);

				LocalVariables[index++] = operand;
			}
		}

		/// <summary>
		/// Sets the protected regions.
		/// </summary>
		/// <param name="protectedRegions">The protected regions.</param>
		public void SetProtectedRegions(IList<ProtectedRegion> protectedRegions)
		{
			ProtectedRegions = protectedRegions;
		}

		/// <summary>
		/// Initializes the type.
		/// </summary>
		protected virtual void InitializeType()
		{
			if (Method.IsSpecialName && Method.IsRTSpecialName && Method.IsStatic && Method.Name == ".cctor")
			{
				typeInitializer = Compiler.PostCompilePipeline.FindFirst<TypeInitializerSchedulerStage>();

				if (typeInitializer == null)
					return;

				typeInitializer.Schedule(Method);
			}
		}

		/// <summary>
		/// Called before the method compiler begins compiling the method.
		/// </summary>
		protected virtual void BeginCompile()
		{
		}

		/// <summary>
		/// Called after the method compiler has finished compiling the method.
		/// </summary>
		protected virtual void EndCompile()
		{
		}

		/// <summary>
		/// Gets the stage.
		/// </summary>
		/// <param name="stageType">Type of the stage.</param>
		/// <returns></returns>
		public IPipelineStage GetStage(Type stageType)
		{
			foreach (IPipelineStage stage in Pipeline)
			{
				if (stageType.IsInstanceOfType(stage))
				{
					return stage;
				}
			}

			return null;
		}

		public string FormatStageName(IPipelineStage stage)
		{
			return "[" + Pipeline.GetPosition(stage).ToString("00") + "] " + stage.Name;
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return Method.ToString();
		}

		#endregion Methods
	}
}
