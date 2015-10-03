﻿//
// Copyright (c) Microsoft Corporation.    All rights reserved.
//

//#define ALLOW_PAUSE


namespace Microsoft.CortexM3OnCMSISCore
{
    using Microsoft.Zelig.Runtime.TargetPlatform.ARMv7;

    using RT    = Microsoft.Zelig.Runtime;
    using CMSIS = Microsoft.DeviceModels.Chipset.CortexM3;


    public abstract class Peripherals : RT.Peripherals
    {

        //
        // State
        //

        //
        // Helper Methods
        //

        public override void Initialize()
        {
            RT.BugCheck.AssertInterruptsOff();
            
            // TODO: move to configuration, we set SysTick priority just below highest possible 
            CMSIS.NVIC.SetPriority( ProcessorARMv7M.IRQn_Type.SVCall_IRQn, ProcessorARMv7M.c_Priority__SVCCall ); 

            // TODO: move to configuration, we set SysTick priority just below SVC  
            CMSIS.NVIC.SetPriority( ProcessorARMv7M.IRQn_Type.SysTick_IRQn, ProcessorARMv7M.c_Priority__SysTick ); 

            // TODO: move to configuration, we set the PendSV priority to the lowest 
            CMSIS.NVIC.SetPriority( ProcessorARMv7M.IRQn_Type.PendSV_IRQn , ProcessorARMv7M.c_Priority__PendSV ); 
        }
        
        public override void Activate()
        {
            CMSIS.Drivers.InterruptController.Instance.Initialize();
            CMSIS.Drivers.ContextSwitchTimer.Instance.Initialize();
        }

        public override void EnableInterrupt( uint index )
        {
        }

        public override void DisableInterrupt( uint index )
        {
        }

        public override void CauseInterrupt()
        {
            //ProcessorARMv7M.InitiateContextSwitch( ); 
            Drivers.InterruptController.Instance.CauseInterrupt( ); 
        }

        public override void ContinueUnderNormalInterrupt(Continuation dlg)
        {
            Drivers.InterruptController.Instance.ContinueUnderNormalInterrupt(dlg);
        }

        public override void WaitForInterrupt()
        {

            while (true)
            {
                ProcessorARMv7M.WaitForEvent( );
            }
            
            //ProcessorARMv7M.InitiateContextSwitch( ); 
            //while(true) ;
        }

        public override void ProcessInterrupt()
        {
            using (RT.SmartHandles.SwapCurrentThreadUnderInterrupt hnd = RT.ThreadManager.InstallInterruptThread())
            {
                Drivers.InterruptController.Instance.ProcessInterrupt();
            }
        }

        [RT.MemoryRequirements(RT.MemoryAttributes.RAM)]
        public override void ProcessFastInterrupt()
        {
            using (RT.SmartHandles.SwapCurrentThreadUnderInterrupt hnd = RT.ThreadManager.InstallFastInterruptThread())
            {
                Drivers.InterruptController.Instance.ProcessFastInterrupt();
            }
        }

        public override ulong GetPerformanceCounterFrequency()
        {
            return 1000000;
        }

        [RT.Inline]
        [RT.DisableNullChecks()]
        public override uint ReadPerformanceCounter()
        {
            // TODO: use a different timer
            return Drivers.ContextSwitchTimer.Instance.CurrentTimeRaw;
        }
    }
}
