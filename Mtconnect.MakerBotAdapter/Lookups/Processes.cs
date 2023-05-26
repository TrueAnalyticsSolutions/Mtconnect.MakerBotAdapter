using Mtconnect.AdapterSdk.DataItemValues;
using System.Collections.Generic;
using System.Linq;

namespace Mtconnect.MakerBotAdapter.Lookups
{
    public enum PrinterStep
    {
        initializing,
        initial_heating,
        final_heating,
        cooling,
        homing,
        position_found,
        preheating,
        calibrating,
        end_sequence,
        cancelling,
        preheating_loading,
        preheating_resuming,
        preheating_unloading,
        stopping_filament,
        cleaning_up,
        loading_print_tool,
        waiting_for_file,
        running,
        extrusion,
        loading_filament,
        unloading_filament,
        transfer,
        downloading,
        verify_firmware,
        writing,
        printing,
        suspending,
        suspended,
        unsuspending,
        clear_build_plate,
        clear_filament,
        remove_filament,
        failed,
        error_step,
        completed
    }


}
