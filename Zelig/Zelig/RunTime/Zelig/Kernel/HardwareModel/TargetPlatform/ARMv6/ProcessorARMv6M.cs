﻿//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//


namespace Microsoft.Zelig.Runtime.TargetPlatform.ARMv6
{
    using System;
    using System.Runtime.InteropServices;

    using TS = Microsoft.Zelig.Runtime.TypeSystem;
    using RT = Microsoft.Zelig.Runtime;


    public abstract partial class ProcessorARMv6M : Runtime.Processor
    {
        public enum IRQn_Type : int
        {
            //  Cortex-M Processor IRQ number, from cmsis implementation
            Reset_IRQn              = -42,      /*!< Exception#: 1 Reset (not actually defined as an IRQn_Type)         */
            NonMaskableInt_IRQn     = -14,      /*!< Exception#: 2 Non Maskable Interrupt                               */
            HardFault_IRQn          = -13,      /*!< Exception#: 3 Non Maskable Interrupt                               */
            Reserved_IRQn12         = -12,      /*!< Exception#: 4 Reserved                                             */
            Reserved_IRQn11         = -11,      /*!< Exception#: 5 Reserved                                             */
            Reserved_IRQn10         = -10,      /*!< Exception#: 6 Reserved                                             */
            Reserved_IRQn9          = -9,       /*!< Exception#: 7 Reserved                                             */
            Reserved_IRQn8          = -8,       /*!< Exception#: 8 Reserved                                             */
            Reserved_IRQn7          = -7,       /*!< Exception#: 9 Reserved                                             */
            Reserved_IRQn6          = -6,       /*!< Exception#: 10 Reserved                                            */
            SVCall_IRQn             = -5,       /*!< Exception#: 11 Cortex-M0[plus] SV Call Interrupt                   */
            Reserved_IRQn4          = -4,       /*!< Exception#: 12 Cortex-M0[plus] Debug Monitor Interrupt             */
            Reserved_IRQn3          = -3,       /*!< Exception#: 13 Reserved                                            */
            PendSV_IRQn             = -2,       /*!< Exception#: 14 Cortex-M0[plus] Pend SV Interrupt                   */
            SysTick_IRQn_Optional   = -1,       /*!< Exception#: 15 Cortex-M0[plus] System Tick Interrupt (OPTIONAL!)   */
            //--//
            AnyInterrupt16          =  0,
            
            //--//

            Invalid                 = 0xFFFF,
        }

        public enum ISR_NUMBER : uint
        {
            //  Cortex-M Processor exception Numbers, as reported by the IPSR
            ThreadMode          =   0,
            Reset               =   1,
            NMI                 =   2,
            HardFault           =   3,
            Reserved4           =   4,
            Reserved5           =   5,
            Reserved6           =   6,
            Reserved7           =   7,
            Reserved8           =   8,
            Reserved9           =   9,
            Reserved10          =  10,
            SVCall              =  11,
            ReservedForDebug    =  12,
            Reserved13          =  13,
            PendSV              =  14,
            SysTick             =  15,
            //--//
            Peripheral          =  16, 
            Last                = 240, 
        }
        
        public enum SVC_Code : byte
        {
            SupervisorCall__LongJump                        = 0x11,
            SupervisorCall__StartThreads                    = 0x12,
            SupervisorCall__RetireThread                    = 0x13,
            SupervisorCall__SnapshotProcessModeRegisters    = 0x14,
        }

        public struct StandardFrame
        {
            public UIntPtr R0;
            public UIntPtr R1;
            public UIntPtr R2;
            public UIntPtr R3;
            public UIntPtr R12;
            public UIntPtr LR;
            public UIntPtr PC;
            public UIntPtr PSR;
        }

        //
        // Exception priorities
        //
        
        public const uint c_Priority__MASK                  = 0x000000FFu;
        public const uint c_Priority__NeverDisabled         = 0x00000000u;
        public const uint c_Priority__Highest               = 0x00000001u;
        public const uint c_Priority__Lowest                = 0x000000FFu;
        public const uint c_Priority__HigherThanAnyWeOwn    = 0x00000004u;
        public const uint c_Priority__SVCCall               = 0x00000005u;
        public const uint c_Priority__Default               = 0x00000007u;
        public const uint c_Priority__SystemTimer           = c_Priority__Default;
        public const uint c_Priority__SysTick               = c_Priority__Default;
        public const uint c_Priority__PendSV                = 0x0000000Eu;

        //--//

        #region Registers and Fields Definitions

        //
        // Interrupts management
        // In the simplest case, we will use the PRIMASK against a BASEPRI of 0x00000000, which is the reset value 
        //
        public const uint c_PRIMASK__InterruptsOff      = 0x00000001;
        public const uint c_PRIMASK__InterruptsOn       = 0x00000000;
        public const uint c_FAULTMASK__FaultsOff        = 0x00000001;
        public const uint c_FAULTMASK__FaultsOn         = 0x00000000;
        public const uint c_FAULTMASK__AreFaultsOff     = c_FAULTMASK__FaultsOff << 1;

        //
        // PSR register
        //

        public const uint c_psr_InitialValue            = 0x01000000; // just Thumb state set

        //
        // CONTROL register
        //
        
        public const uint c_CONTROL__MASK               = 0x00000003;
        public const uint c_CONTROL__MASK_PRIVILEGE     = 0x00000001;

        //
        // stack pointer
        //
        public const  int c_CONTROL__SPSEL_SHIFT        = 1;
        public const uint c_CONTROL__SPSEL_MASK         = 0x1u << c_CONTROL__SPSEL_SHIFT;
        public const uint c_CONTROL__SPSEL_MSP          = 0x0u << c_CONTROL__SPSEL_SHIFT;
        public const uint c_CONTROL__SPSEL_PSP          = 0x1u << c_CONTROL__SPSEL_SHIFT;

        //
        // privilege
        //
        public const  int c_CONTROL__nPRIV_SHIFT        = 0;
        public const uint c_CONTROL__nPRIV_MASK         = 0x1u << c_CONTROL__nPRIV_SHIFT;
        public const uint c_CONTROL__nPRIV_PRIV         = 0x0u << c_CONTROL__nPRIV_SHIFT;
        public const uint c_CONTROL__nPRIV_UNPRIV       = 0x1u << c_CONTROL__nPRIV_SHIFT;
        
        //
        // Modes
        // 
        public const uint c_CONTROL__MODE__HNDLR        = c_CONTROL__SPSEL_MSP | c_CONTROL__nPRIV_PRIV;
        public const uint c_CONTROL__MODE__THRD_PRIV    = c_CONTROL__SPSEL_PSP | c_CONTROL__nPRIV_PRIV;
        public const uint c_CONTROL__MODE__THRD_UNPRIV  = c_CONTROL__SPSEL_PSP | c_CONTROL__nPRIV_UNPRIV;

        //
        // EXC_RETURN 
        //
        public const uint c_MODE_RETURN__HANDLER_MSP    = 0xFFFFFFF1; // handler will return in handler mode using the MSP
        public const uint c_MODE_RETURN__THREAD_MSP     = 0xFFFFFFF9; // handler will return in thread mode using the MSP
        public const uint c_MODE_RETURN__THREAD_PSP     = 0xFFFFFFFD; // handler will return in thread mode using the PSP

        //
        // SCR
        //
        public const uint c_SCR__MASK                   = 0x0000001F;

        public const  int c_SCR__SLEEPONEXIT__SHIFT     = 1; 
        public const uint c_SCR__SLEEPONEXIT__MASK      = 1u << c_SCR__SLEEPONEXIT__SHIFT;
        public const uint c_SCR__SLEEPONEXIT__NO_SLEEP  = 0u << c_SCR__SLEEPONEXIT__SHIFT; 
        public const uint c_SCR__SLEEPONEXIT__SLEEP     = 1u << c_SCR__SLEEPONEXIT__SHIFT;

        public const  int c_SCR__SLEEPDEEP__SHIFT       = 2; 
        public const uint c_SCR__SLEEPDEEP__MASK        = 1u << c_SCR__SLEEPDEEP__SHIFT;
        public const uint c_SCR__SLEEPDEEP__SLEEP       = 0u << c_SCR__SLEEPDEEP__SHIFT; 
        public const uint c_SCR__SLEEPDEEP__DEEP        = 1u << c_SCR__SLEEPDEEP__SHIFT;  

        public const  int c_SCR__SEVONPEND__SHIFT       = 4; 
        public const uint c_SCR__SEVONPEND__MASK        = 1u << c_SCR__SEVONPEND__SHIFT;
        public const uint c_SCR__SEVONPEND__ENONLY      = 0u << c_SCR__SEVONPEND__SHIFT; 
        public const uint c_SCR__SEVONPEND__ALL         = 1u << c_SCR__SEVONPEND__SHIFT; 

        //
        // CCR
        //
        public const uint c_CCR__MASK                   = 0x000003FF;
        
        public const  int c_CCR__UNALIGN_TRP__SHIFT     = 3; 
        public const uint c_CCR__UNALIGN_TRP__MASK      = 1u << c_CCR__UNALIGN_TRP__SHIFT;
        public const uint c_CCR__UNALIGN_TRP__NOTRAP    = 0u << c_CCR__UNALIGN_TRP__SHIFT; 
        public const uint c_CCR__UNALIGN_TRP__TRAP      = 1u << c_CCR__UNALIGN_TRP__SHIFT;

        public const  int c_CCR__STKALIGN__SHIFT        = 9; 
        public const uint c_CCR__STKALIGN__MASK         = 1u << c_CCR__STKALIGN__SHIFT;
        public const uint c_CCR__STKALIGN__4            = 0u << c_CCR__STKALIGN__SHIFT; 
        public const uint c_CCR__STKALIGN__8            = 1u << c_CCR__STKALIGN__SHIFT;
        
        public const uint c_CCR_STD_CONFIG_4            = (c_CCR__STKALIGN__4 | c_CCR__UNALIGN_TRP__NOTRAP) & c_CCR__MASK; 
        public const uint c_CCR_STD_CONFIG_8            = (c_CCR__STKALIGN__8 | c_CCR__UNALIGN_TRP__NOTRAP) & c_CCR__MASK;

        //
        // ICSR
        //
        public const uint c_ICSR__MASK                  = 0xFFFFFFFF;
        public const uint c_ICSR__ALLOWED_MASK          = 0x9E400000;
        
        public const  int c_ICSR__PENDSTCLR__SHIFT      = 25;
        public const uint c_ICSR__PENDSTCLR__MASK       = 1u << c_ICSR__PENDSTCLR__SHIFT;
        public const uint c_ICSR__PENDSTCLR__SET        = 1u << c_ICSR__PENDSTCLR__SHIFT;
    
        public const  int c_ICSR__PENDSTSET__SHIFT      = 26;
        public const uint c_ICSR__PENDSTSET__MASK       = 1u << c_ICSR__PENDSTSET__SHIFT;
        public const uint c_ICSR__PENDSTSET__SET        = 1u << c_ICSR__PENDSTSET__SHIFT;
        
        public const  int c_ICSR__PENDSVCLR__SHIFT      = 27;
        public const uint c_ICSR__PENDSVCLR__MASK       = 1u << c_ICSR__PENDSVCLR__SHIFT;
        public const uint c_ICSR__PENDSVCLR__SET        = 1u << c_ICSR__PENDSVCLR__SHIFT;

        public const  int c_ICSR__PENDSVSET__SHIFT      = 28;
        public const uint c_ICSR__PENDSVSET__MASK       = 1u << c_ICSR__PENDSVSET__SHIFT;
        public const uint c_ICSR__PENDSVSET__SET        = 1u << c_ICSR__PENDSVSET__SHIFT;

        public const  int c_ICSR__NMIPENDSET__SHIFT     = 31;
        public const uint c_ICSR__NMIPENDSET__MASK      = 1u << c_ICSR__NMIPENDSET__SHIFT;
        public const uint c_ICSR__NMIPENDSET__SET       = 1u << c_ICSR__NMIPENDSET__SHIFT;
        
        #endregion 

        //--//

        [TS.WellKnownType( "Microsoft_Zelig_ARMv6_MethodWrapper" )]
        public sealed class MethodWrapper : AbstractMethodWrapper
        {

            [Inline]
            [DisableNullChecks( ApplyRecursively = true )]
            public override void Prologue( string typeFullName,
                                           string methodFullName,
                                           TS.MethodRepresentation.BuildTimeAttributes attribs )
            {

            }

            [Inline]
            [DisableNullChecks( ApplyRecursively = true )]
            public unsafe override void Prologue( string typeFullName,
                                                  string methodFullName,
                                                  TS.MethodRepresentation.BuildTimeAttributes attribs,
                                                  HardwareException he )
            {

            }

            [Inline]
            [DisableNullChecks( ApplyRecursively = true )]
            public override void Epilogue( string typeFullName,
                                           string methodFullName,
                                           TS.MethodRepresentation.BuildTimeAttributes attribs )
            {

            }

            [Inline]
            [DisableNullChecks( ApplyRecursively = true )]
            public unsafe override void Epilogue( string typeFullName,
                                                  string methodFullName,
                                                  TS.MethodRepresentation.BuildTimeAttributes attribs,
                                                  HardwareException he )
            {

            }

        }

        //
        // Helper Methods
        //

        public override void InitializeProcessor( )
        {
            //
            // We want to run ISRs in privilged Handler mode using the Main Stack Pointer and all the other tasks 
            // in privileged Thread mode using the Process Stack Pointer. 
            //
            // We will assume that native context switching is possible when processor initialization is carried out in
            // Handler/Privileged mode. Of course that is not a complete guarantee. After carrying out the initailization of the 
            // idle thread task, we will let the initialization thread return to thread mode upon first context switch as per classic 
            // technique mentioned below, from ARM reference manual. As switching the mode is carried out naturally by 
            // setting the appropriate flag, there is nothing else we need to do at initialization time. See context switch code 
            // in thread manager as well.

            //
            // From ARMv-7M Architecture Reference Manual. 
            // 
            // 
            // By default, Thread mode uses the MSP. To switch the stack pointer used in Thread mode to the
            // PSP, either:
            // - use the MSR instruction to set the Active stack pointer bit to 1
            // - perform an exception return to Thread mode with the appropriate EXC_RETURN value
            //

            ////// 
            //////  
            ////// 
            //////  Table 2-17 Exception return behavior
            //////
            //////  EXC_RETURN      Description
            //////  =========================================================================
            //////  0xFFFFFF[F|E]1  Return to Handler mode.
            //////                  Exception return gets state [and FP state] from the main 
            //////                  stack (MSP).
            //////                  Execution uses MSP after return.
            ////// 
            //////  0xFFFFFF[F|E]9  Return to Thread mode.
            //////                  Exception Return get state [and FP state] from the main 
            //////                  stack (MSP).
            //////                  Execution uses MSP after return.
            ////// 
            //////  0xFFFFFF[F|E]D  Return to Thread mode.
            //////                  Exception return gets state [and FP state] from the process 
            //////                  stack (PSP).
            //////                  Execution uses PSP after return.
            ////// 
            //////  All other values Reserved.
            ////// 
            
            //
            // Disable interrupts, but not faults 
            //
            DisableInterrupts( );
            
            //
            // Ensure privileged Handler mode to boot
            //
            
            if(!VerifyHandlerMode())
            {
                RT.BugCheck.Log( "Cannot bootstrap in Thread mode" );
                RT.BugCheck.Assert( false, RT.BugCheck.StopCode.FailedBootstrap );
            }

            //
            // Enforce reset behavior: 
            // - only enabled interrupts or events can wakeup the processor, disabled interrupts are excluded
            // - sleep by turning off clock to main proc (not deep) 
            // - do not sleep when returning to thread mode
            //
            SetSystemControlRegister(   c_SCR__SLEEPONEXIT__NO_SLEEP    |  
                                        c_SCR__SLEEPDEEP__SLEEP         | 
                                        c_SCR__SEVONPEND__ENONLY        ); 
            
            // Enforce 8 bytes alignment
            //
            SetCCR( c_CCR_STD_CONFIG_4 );
        }
                
        internal static void SetCCR( uint val )
        {
            // NOTE: LWIP uses memory functions on addresses which are not 8 byte aligned
            // This prevents faults from occurring from those memory accesses

            CUSTOM_STUB_SCB_set_CCR( val );
        }

        internal static void RaiseSystemHandler( uint mask )
        {
            BugCheck.Assert( ((mask & ~(c_ICSR__ALLOWED_MASK)) == 0), BugCheck.StopCode.IncorrectArgument );

            CUSTOM_STUB_SCB_ICSR_RaiseSystemException( mask );
        }

        public static unsafe void RaiseSupervisorCall( SVC_Code code )
        {
            switch(code)
            {
                case SVC_Code.SupervisorCall__LongJump:
                    CUSTOM_STUB_RaiseSupervisorCallForLongJump( );
                    break;
                case SVC_Code.SupervisorCall__StartThreads:
                    CUSTOM_STUB_RaiseSupervisorCallForStartThreads( );
                    break;
                case SVC_Code.SupervisorCall__RetireThread:
                    CUSTOM_STUB_RaiseSupervisorCallForRetireThread( );
                    break;
                case SVC_Code.SupervisorCall__SnapshotProcessModeRegisters:                   
                    //
                    // Cause a SVC call to transition to Handler mode and 
                    // snapshot the Processmode registers
                    //
                    CUSTOM_STUB_RaiseSupervisorCallForSnapshotProcessModeRegisters( );                        
                    break;
                default:
                    throw new ArgumentException( "Request SVC does not exists or is not supported" );
            }


        }
        
        public static bool IsAnyExceptionActive( )
        {
            return ( ( (ISR_NUMBER)CUSTOM_STUB_SCB_IPSR_GetCurrentISRNumber( ) ) != ISR_NUMBER.ThreadMode ); 
        }
        
        public static void InitiateContextSwitch( )
        {
            RaiseSystemHandler( c_ICSR__PENDSTSET__SET ) ;
        }

        public static void CompleteContextSwitch( )
        {
            RaiseSystemHandler( c_ICSR__PENDSVSET__SET ) ;
        }

        internal void SetSystemControlRegister( uint flags )
        {
            CUSTOM_STUB_SCB_SCR_SetSystemControlRegister( flags );
        }

        public static void WaitForEvent( )
        {
            CMSIS_STUB_POWER_WaitForEvent( ); 
        }

        public static void SendEvent( )
        {
            CMSIS_STUB_POWER_SendEvent( ); 
        }

        public static void WaitForInterrupt( )
        {
            CMSIS_STUB_POWER_WaitForInterrupt( ); 
        }

        //--//
        
        private static void DisableSystemHandler( IRQn_Type ex )
        {
            BugCheck.Assert( (ex < 0), BugCheck.StopCode.IncorrectArgument );
            
            uint mask = 0;

            switch(ex)
            {
                case IRQn_Type.Reset_IRQn         :
                case IRQn_Type.NonMaskableInt_IRQn:
                case IRQn_Type.HardFault_IRQn     :  
                    // Cannot enable or disable NMI, Reset or HardFault
                    //BugCheck.Assert( false, BugCheck.StopCode.IllegalMode );
                    break;
                case IRQn_Type.SVCall_IRQn          :
                case IRQn_Type.PendSV_IRQn          :
                case IRQn_Type.SysTick_IRQn_Optional:
                    // NOT IMPLEMENTED: call NVIC
                    //BugCheck.Assert( false, BugCheck.StopCode.IncorrectArgument );
                    break;
                default:
                    BugCheck.Assert( false, BugCheck.StopCode.IncorrectArgument );
                    break;
            }

            CUSTOM_STUB_SCB_SHCRS_DisableSystemHandler( mask );
        }
        
        //
        // Cache
        // 

        public override UIntPtr GetCacheableAddress( UIntPtr ptr )
        {
            // Cortex-M7 actually has a cache, so override the method
            return ptr;
        }

        public override UIntPtr GetUncacheableAddress( UIntPtr ptr )
        {
            // Cortex-M7 actually has a cache, so override the method
            return ptr;
        }    
   
        public override void FlushCacheLine( UIntPtr target )
        {
        }

        //--//
        
        [Inline]
        public override bool AreInterruptsDisabled( )
        {
            return GetPriMaskRegister() == 1;
        }

        public override bool AreAllInterruptsDisabled( )
        {
            return AreInterruptsDisabled( ); 
        }
        
        public override void Breakpoint( )
        {
            Breakpoint( 0xa5a5a5a5 ); 
        }
        
        //--//--//

        public static uint EnableInterrupts( )
        {
            return SetPriMaskRegister( c_PRIMASK__InterruptsOn );
        }
        
        public static uint DisableInterrupts( )
        {
            return SetPriMaskRegister( c_PRIMASK__InterruptsOff );
        }

        public static bool VerifyHandlerMode( )
        {
            uint control = CMSIS_STUB_SCB__get_CONTROL( );
            
            return ((control & c_CONTROL__SPSEL_MASK) == c_CONTROL__SPSEL_MSP);
        }
        
        //--//
        
        [Inline]
        internal static uint GetPriMaskRegister( )
        {
            return CMSIS_STUB_SCB__get_PRIMASK( );
        }

        [Inline]
        internal static uint SetPriMaskRegister( uint basepri )
        {
            return CMSIS_STUB_SCB__set_PRIMASK( basepri );
        }

        //--//
        
        [Inline]
        protected static unsafe UIntPtr GetMainStackPointerAtReset()
        {
            return new UIntPtr(CMSIS_STUB_SCB__get_MSP_ResetValue());
        }

        internal static UIntPtr GetMainStackPointerBottom()
        {
            UIntPtr stackBottom = AddressMath.Decrement(GetMainStackPointerAtReset(), GetMainStackSize());

            return stackBottom;
        }

        [Inline]
        protected static uint GetMainStackSize()
        {
            return CMSIS_STUB_SCB__get_MSP_StackSize();
        }

        [Inline]
        protected static UIntPtr GetMainStackPointer( )
        {
            return new UIntPtr( CMSIS_STUB_SCB__get_MSP() );
        }
            
        [Inline]
        protected static void SetMainStackPointer( UIntPtr topOfMainStack )
        {
            CMSIS_STUB_SCB__set_MSP( topOfMainStack.ToUInt32() );
        }
            
        [Inline]
        protected static UIntPtr GetProcessStackPointer( )
        {
            return new UIntPtr( CMSIS_STUB_SCB__get_PSP( ) );
        }
            
        [Inline]
        protected static void SetProcessStackPointer( UIntPtr topOfProcessStack )
        {
            CMSIS_STUB_SCB__set_PSP( topOfProcessStack.ToUInt32() );
        }
        
        protected static void SwitchToHandlerPrivilegedMode( )
        {
            uint control = CMSIS_STUB_SCB__get_CONTROL( );
            
            control &= ~c_CONTROL__MASK; 
            control |=  c_CONTROL__MODE__HNDLR;

            CMSIS_STUB_SCB__set_CONTROL( control & c_CONTROL__MASK );
        }
        
        protected static void SwitchToThreadUnprivilegedMode( )
        {
            uint control = CMSIS_STUB_SCB__get_CONTROL( );
            
            control &= ~c_CONTROL__MASK; 
            control |= c_CONTROL__MODE__THRD_UNPRIV;

            CMSIS_STUB_SCB__set_CONTROL( control & c_CONTROL__MASK );
        }
        
        protected static void SwitchToThreadPrivilegedMode( )
        {
            uint control = CMSIS_STUB_SCB__get_CONTROL( );
            
            control &= ~c_CONTROL__MASK; 
            control |= c_CONTROL__MODE__THRD_PRIV;

            CMSIS_STUB_SCB__set_CONTROL( control & c_CONTROL__MASK );
        }
        
        protected static void SwitchToPrivilegedMode( )
        {
            uint control = CMSIS_STUB_SCB__get_CONTROL( );

            control &= ~c_CONTROL__MASK_PRIVILEGE; 
            control |=  c_CONTROL__nPRIV_PRIV;

            CMSIS_STUB_SCB__set_CONTROL( control & c_CONTROL__MASK );
        }
        
        protected static void SwitchToUnprivilegedMode( )
        {
            uint control = CMSIS_STUB_SCB__get_CONTROL( );
            
            control &= ~c_CONTROL__MASK_PRIVILEGE; 
            control |=  c_CONTROL__nPRIV_UNPRIV;

            CMSIS_STUB_SCB__set_CONTROL( control & c_CONTROL__MASK );
        }

        protected static void SetExcReturn( uint ret )
        {
            CUSTOM_STUB_SetExcReturn( ret );
        }
        
        //
        // Fault diagnostic
        //

        protected static uint GetProgramCounter()
        {
            return CUSTOM_STUB_GetProgramCounter( ); 
        }
        
        protected static bool DebuggerConnected()
        {
            return false; 
        }
        
        [RT.Inline]
        protected static unsafe StandardFrame* PointerToStdFrame( UIntPtr SP )
        {
            return (StandardFrame*)SP.ToPointer( );
        }

        //
        // All overridable exceptions
        //

        //////[RT.BottomOfCallStack( )]
        //////[RT.HardwareExceptionHandler( RT.HardwareException.NMI )]
        //////[RT.ExportedMethod]
        //////private static void NMI_Handler( )
        //////{
        //////    //
        //////    // The processor clears the FAULTMASK bit to 0 on exit from any exception handler except the NMI handler.
        //////    //

        //////    EnableFaults( );
        //////}

        /// <summary>
        /// Hard Fault is caused by Bus Fault, Memory Management Fault, or Usage Fault if their handler 
        /// cannot be executed.
        /// </summary>
        [RT.CapabilitiesFilter(RequiredCapabilities = TargetModel.ArmProcessor.InstructionSetVersion.Platform_Version__ARMv6M)]
        [RT.HardwareExceptionHandler( RT.HardwareException.Fault )]
        [RT.ExportedMethod]
        private static void HardFault_Handler_Zelig( uint sp )
        {
            while(true)
            {
                Peripherals.Instance.WaitForInterrupt();
            }
        }
        
        //////[RT.BottomOfCallStack( )]
        //////[RT.HardwareExceptionHandler( RT.HardwareException.Reset )]
        //////[RT.NoReturn]
        //////[RT.ExportedMethod]
        //////private static void Reset_Handler( )
        //////{
        //////}
        
        //////[RT.BottomOfCallStack( )]
        //////[RT.HardwareExceptionHandler( RT.HardwareException.Debug )]
        //////[RT.NoReturn]
        //////[RT.ExportedMethod]
        //////private static void DebugMon_Handler( )
        //////{
        //////}
        
        //
        // We will implement the intrernal methods below with CMSIS-Core or custom stubs
        //      
        
        [DllImport( "C" )]
        internal static extern uint CUSTOM_STUB_DebuggerConnected( );
        
        [DllImport( "C" )]
        internal static extern uint CUSTOM_STUB_GetProgramCounter( );
               
        [DllImport( "C" )]
        internal static extern uint CMSIS_STUB_SCB__get_CONTROL( );

        [DllImport( "C" )]
        internal static extern void CMSIS_STUB_SCB__set_CONTROL( uint control );

        [DllImport( "C" )]
        internal static extern uint CMSIS_STUB_SCB__get_IPSR( );

        [DllImport( "C" )]
        internal static extern uint CMSIS_STUB_SCB__get_APSR( );
        
        [DllImport( "C" )]
        internal static extern uint CMSIS_STUB_SCB__get_xPSR( );

        [DllImport( "C" )]
        internal static extern uint CMSIS_STUB_SCB__get_PSP( );

        [DllImport( "C" )]
        internal static extern void CMSIS_STUB_SCB__set_PSP( uint topOfProcStack );

        [DllImport("C")]
        internal static unsafe extern void* CMSIS_STUB_SCB__get_MSP_ResetValue();

        [DllImport("C")]
        internal static extern uint CMSIS_STUB_SCB__get_MSP_StackSize();

        [DllImport( "C" )]
        internal static extern uint CMSIS_STUB_SCB__get_MSP( );

        [DllImport( "C" )]
        internal static extern void CMSIS_STUB_SCB__set_MSP( uint topOfMainStack );

        [DllImport( "C" )]
        internal static extern uint CMSIS_STUB_SCB__get_PRIMASK( );

        [DllImport( "C" )]
        internal static extern uint CMSIS_STUB_SCB__set_PRIMASK( uint priMask );
        
        
        [DllImport( "C" )]
        internal static extern void CMSIS_STUB_SCB__Enable_Irq( );

        [DllImport( "C" )]
        internal static extern void CMSIS_STUB_SCB__Disable_Irq( );

        //
        // !!! Cortex M0 only !!!
        //

        [DllImport( "C" )]
        internal static extern void CUSTOM_STUB_SCB_set_CCR( uint value );

        [DllImport( "C" )]
        internal static extern void CUSTOM_STUB_SCB_SHCRS_DisableSystemHandler( uint ex );
        
        [DllImport( "C" )]
        internal static extern void CUSTOM_STUB_SCB_ICSR_RaiseSystemException( uint ex );

        [DllImport( "C" )]
        internal static extern void CUSTOM_STUB_RaiseSupervisorCallForLongJump( );
 
        [DllImport( "C" )]
        internal static extern void CUSTOM_STUB_RaiseSupervisorCallForStartThreads( );

        [DllImport( "C" )]
        internal static extern void CUSTOM_STUB_RaiseSupervisorCallForRetireThread( );

        [DllImport( "C" )]
        internal static extern void CUSTOM_STUB_RaiseSupervisorCallForSnapshotProcessModeRegisters( );
                
        [DllImport( "C" )]
        private static extern void CUSTOM_STUB_SetExcReturn( uint ret );
        
        [DllImport( "C" )]
        internal static extern int CUSTOM_STUB_SCB_IPSR_GetCurrentISRNumber( );

        [DllImport( "C" )]
        internal static extern int CUSTOM_STUB_SCB_SCR_GetSystemControlRegister( );

        [DllImport( "C" )]
        internal static extern void CUSTOM_STUB_SCB_SCR_SetSystemControlRegister( uint scr ); 

        [DllImport( "C" )]
        private static extern void CMSIS_STUB_POWER_WaitForEvent( ); 

        [DllImport( "C" )]
        private static extern void CMSIS_STUB_POWER_SendEvent( ); 

        [DllImport( "C" )]
        private static extern void CMSIS_STUB_POWER_WaitForInterrupt( );

        //--//

        [DllImport( "C" )]
        protected static extern unsafe uint* CUSTOM_STUB_FetchSoftwareFrameSnapshot( );

        //--//
        //--//
        //--//
        
        //
        // Utility methods 
        //

        //[TS.WellKnownMethod( "ProcessorARM_Breakpoint" )]

        [DllImport( "C" )]
        public static extern void Breakpoint( uint value );

        [DllImport( "C" )]
        public static extern void Nop( );
    }
}
