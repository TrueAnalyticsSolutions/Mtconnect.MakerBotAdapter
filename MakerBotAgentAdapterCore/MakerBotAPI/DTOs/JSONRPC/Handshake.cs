
namespace MakerBotAgentAdapterCore.MakerBotAPI.DTOs.RPC {
  public class Handshake {
    public _handshakeResult result;
    public string jsonrpc;
    public int id;

    public class _handshakeResult {
      public string commit;
      public string machine_type;
      public string ip;
      public string iserial;
      public string port;
      public int vid;
      public int pid;
      public string builder;
      public string machine_name;
      public string api_version;
      public int ssl_port;
      public string motor_driver_version;
      public string bot_type;
    }
  }
  public class SystemInformation {
    public SystemInformationResult result;
    public string jsonrpc;
    public int id;

    public class SystemInformationResult {
      public string machine_type;
      public object toolheads;
      public string ip;
      public bool has_been_connected_to;
      public string api_version;
      public Firmware_Version firmware_version;
      public CurrentProcess current_process;
      public string machine_name;
      public bool sound;
      public string bot_type;
      public object disabled_errors;
      public string auto_unload;

      public class Toolheads{
        public Extruder extruder;

        public class Extruder{
          public int current_temperature;
          public int tool_id;
          public bool tool_present;
          public int index;
          public int error;
          public int target_temperature;
          public bool filament_presence;
          public bool preheating;
        }
      }
      public class CurrentProcess{
        public object reason;
        public int thing_id;
        public bool cancelled;
        public int elapsed_time;
        public object start_time;
        public string step;
        public bool cancellable;
        public int progress;
        public bool can_print_again;
        public Error error;
        public string username;
        public int id;
        public string[] methods;
        public string filepath;
        public string name;
        public dynamic print_temperatures;
        public int filament_extruded;
        public int time_remaining;
        public bool complete;
        public double[] extrusion_mass_g;
        public double[] extrusion_distance_mm;
        public string filename;
        public double time_estimation;

      }
    }

  }
  public class NotAuthenticated {
    public int id;
    public string jsonrpc;
    public Error error;
  }
  public class Authenticated {
    public string jsonrpc;
    public object result;
    public int id;
  }

  public class WiFiAccessPoints{
    public string jsonrpc;
    public WiFiAccesPoint[] result;
    public int id;

    public class WiFiAccesPoint{
      public string password;
      public string path;
      public string name;
      public int strength;
    }
  }
  public class Authorizations{
    public string jsonrpc;
    public Authorization[] result;
    public int id;

    public class Authorization{
      public int local_auth_count;
      public string username;
      public bool makerbot_account;
    }
  }
  public class Process{
    public string jsonrpc;
    public ProcessResult result;
    public int id;

    public class ProcessResult{
      public object methods;
      public object reason;
      public string name;
      public bool cancelled;
      public string step;
      public bool completed;
      public bool cancellable;
      public Error error;
      public string username;
      public int id;
    }
  }
  public class MachineActionProcess{
    public string jsonrpc;
    public MachineActionProcessResults result;
    public int id;

    public class MachineActionProcessResults{
      public object methods;
      public object @params;
      public object reason;
      public string name;
      public bool cancelled;
      public bool ignore_tool_errors;
      public Error error;
      public string step;
      public bool complete;
      public bool cancellable;
      public string machine_func;
      public string username;
      public int id;

    }
  }
  public class ToolUsageStats{
    public string jsonrpc;
    public ToolUsageStatsResult result;
    public int id;

    public class ToolUsageStatsResult{
      public int serial;
      public double extrusion_mass_g;
      public int extrusion_distance_mm;
      public int refurb_count;
      public int extrusion_time_s;
      public int retract_count;
    }
  }
  public class QueueStatus{
    public string jsonrpc;
    public QueueStatusResult result;
    public int id;

    public class QueueStatusResult{
      public bool queue_open;
      public object queue;
    }
  }
  public class MachineConfig{
    public string jsonrpc;
    public object result;
    public int id;

    public class MachineConfigResult{
      public Acceleration acceleration;
      public string bot_type;
      public GantryConfiguration gantry_configuration;
      public ExtraSlicerSettings extra_slicer_settings;
      public AxesDouble steps_per_mm;
      public AxesDouble start_position;
      public AxesInt max_speed_mm_per_second;
      public int makerbot_generation;
      public AxesInt build_volume;
      public string version;
      public ExtruderProfiles extruder_profiles;

      public class Acceleration{
        public int buffer_size;
        public int split_move_recursion_count;
        public AxesDouble min_speed_change_mm_per_s;
        public AxesInt impulse_speed_limit_mm_per_s;
        public AxesInt rate_mm_per_s_sq;
        public double split_move_distance_mm;
        public AxesInt max_speed_change_mm_per_s;
      }
      public class GantryConfiguration{
        public int travel_speed_xy;
        public int max_fill_speed;
        public int max_inner_shell_speed;
        public int travel_speed_z;
        public int max_outer_shell_speed;
      }
      public class ExtraSlicerSettings{
        public double plate_variability;
      }
      public class ExtruderProfiles{
        public ExtruderProfile mk13;
        public ExtruderProfile mk12;
        public AttachedExtruder[] attached_extruders;
        public dynamic supported_extruders;
        public ExtruderProfile mk13_impla;

        public class AttachedExtruder{
          public bool calibrated;
          public int id;
        }

        public class ExtruderProfile{
          public double nozzle_diameter;
          public ARate max_speed_mm_per_second;
          public ARate steps_per_mm;
          public Materials materials;

          public class Materials{
            public Material pla;
            public Material im_pla;

            public class Material{
              public double feed_diameter;
              public double retract_distance;
              public int temperature;
              public int retract_rate;
              public object acceleration;
              public double ooze_feedstock_distance;
              public double max_flow_rate;
              public int restart_rate;

              public class MaterialAcceleration{
                public ARate min_speed_change_mm_per_s;
                public ARate rate_mm_per_s_sq;
                public double[][] slip_compensation_table;
                public ARate max_speed_change_mm_per_s;
                public ARate impulse_speed_limit_mm_per_s;
              }
            }
          }
        }
      }
    }
  }
  public class AxesDouble {
    public double x;
    public double y;
    public double z;
    public double a;
  }
  public class AxesInt {
    public int x;
    public int y;
    public int z;
    public int a;
  }
  public class AxesBool{
    public bool x;
    public bool y;
    public bool z;
    public bool a;
  }
  public class ARate {
    public double a;
  }
  public class Toolhead {
    public Heater heater;
    public int[] locations;
    public string[] axes;
    public string[] type;
    public string program;

    public class Heater {
      public int print_temperature_default;
      public int sag_temperature_threshold;
      public int rise_timeout_min;
      public int overrun_temperature_threshold;
      public int at_temp_timeout_min;
      public int fan_preload;
      public int rise_temperature_threshold;
      public int preload_tick_delay;
      public int wait_for_target_timeout;
      public int preheat_temperature;
      public int temperature_freeze_timeout_ms;
    }
  }
  public class Config{
    public string jsonrpc;
    public object result;
    public int id;

    public class ConfigResult{
      public ExtraSlicerSettings extra_slicer_settings;
      public int buffer_stall_error_threshold_ticks;
      public double linear_density;
      public int toolhead_count;
      public AxesInt park_prep;
      public Filament filament;
      public int temperature;
      public Home home;
      public dynamic extruder_profiles;
      public string machine_log_level;
      public AxesInt direction_multiplier;
      public double ooze_feedstock_distance;
      public Kaiten kaiten;
      public Suspend suspend;
      public string material_default;
      public AxesInt safe_park;
      public AxesDouble max_speed_mm_per_second;
      public AxesDouble step_per_mm;
      public int sync_stall_error_threshold_ticks;
      public double retract_distance;
      public AxesDouble start_position;
      public int active_tool;
      public bool has_heated_chamber;
      public double feed_diameter;
      public AxesDouble purge_start_distance;
      public bool toolhead_syncing;
      public int retract_rate;
      public AxesInt park;
      public AxesInt build_volume;
      public Leveling leveling;
      public bool machine_use_console_log;
      public AxesInt axis_length;
      public double max_flow_rate;
      public bool single_button_interface;
      public Lights lights;
      public int restart_rate;
      public double nozzle_diameter;
      public Materials materials;
      public Toolheads toolheads;
      public string print_log_filename;
      public int toolhead_sync_delay;
      public GantryInformation gantry_information;
      public int interface_address;
      public Acceleration acceleration;
      public string bot_type;

      public class ExtraSlicerSettings{
        public double plate_variability;
      }
      public class Filament{
        public int unload_distance_two;
        public int load_speed;
        public bool subtract_suspend_retract;
        public int unload_delay;
        public int unload_distance_one;
        public int load_distance;
        public int unload_speed_two;
        public int unload_speed_one;
        public int auto_unload_distance;
      }
      public class Home{
        public int seat_speed_mm_per_s;
        public double extra_z_offset;
        public double timeseries_home_distance;
        public AxesInt direction;
        public double timeseries_home_speed;
        public AxesInt rate_mm_per_s;
        public double nozzle_clearance;
        public int cool_temp_c;
        public double seat_distance_mm;
        public int timeseries_window;
        public int temperature;
        public HomeType type;
        public string method;
        public AxesInt position_mm;
        public AxesDouble rate_mm_per_s_fine;
        public double available_z_offset_adjustment;
        public AxesInt distance_mm;
        public bool pin_progress_during_z_home;
        public double timeseries_start_position_z;
        public AxesInt safe_z_home_position;
        public AxesBool polarity;
        public int timeseries_max_seated_value;
        public double restart_distance_mm;
        public int retract_speed_mm_per_s;
        public double timeseries_sample_rate;
        public MagSense mag_sense;
        public double retract_distance_mm;
        public ZReferencePoint z_reference_point;
        public string z_calibration_hot;
        public int timeseries_retries;
        public bool build_plate_check;
        public string z_endstop_type;
        public AxesDouble move_away_mm;
        public double timeseries_threshold;
        public int timeseries_min_range;
        public double step_size;
        public dynamic per_extruder;

        public class HomeType{
          public string x;
          public string y;
          public string z;
        }
        public class MagSense{
          public int threshold;
          public int deviation;
          public int sample_size;
          public int exponent;
        }
        public class ZReferencePoint{
          public double x;
          public double y;
          public double offset;
        }
      }
    
      public class Kaiten{
        public string thingiverse_api_url;
        public string reflector_url;
        public string machine_type;
        public object analytics_client_id;
        public Components components;
        public int[] configured_tools;
        public FirmwareUpdate firmware_update;
        public string drm_server;
        public bool wait_on_completed;
        public bool reflector_enabled;
        public string[] fre_steps;
        public object[] disabled_errors;
        public Logging logging;
        public bool implicit_bronx_upload;
        public bool keep_calibration_settings;
        public bool image_parity;
        public bool store_tool_usage;
        public int tv_printer_id;
        public bool enable_cloud;
        public int toolhead_error_debounce_time;
        public FontUpdate font_update;
        public bool has_been_connected_to;
        public Mixpanel mixpanel;
        public bool do_not_reset_lcd_settings;
        public double z_adjusted_offset;
        public string first_registered_user;
        public bool requires_z_calibration;
        public string machine_name;
        public bool sound;
        public object analytics_enabled;
        public bool do_seat_tool;
        public bool clear_build_plate;
        public bool do_full_profiling;
        public string auto_unload;

        public class Components{
          public bool udp;
          public bool machine_manager;
          public bool profiling;
          public bool epoll;
          public bool reflector;
          public bool tcp_socket;
          public bool mdns;
          public bool netlog;
          public bool pipe;
          public bool ssl_tcp_socket;
          public bool camera;
          public bool dbus;
          public bool usb;
        }
        public class FirmwareUpdate{
          public string firmware_server_url;
          public string firmware_download_path;
          public string firmware_versions_list;
        }
        public class Logging{
          public string level;
          public bool enabled;
        }
        public class FontUpdate{
          public string font_download_path;
          public string font_server_url;
          public string font_files_list;
        }
        public class Mixpanel{
          public string mixpanel_url;
          public string mixpanel_token;
        }
      }
      public class Suspend{
        public int retract_distance;
        public int restart_distance;
      }
      public class Leveling{
        public double threshold;
        public bool Level_Hot;
        public bool calibrate_z;
        public bool @lock;
        public Positions positions;
        public bool can_switch_sides;
        public double slope_sample_distance;
        public int Timeout_Duration;
        public int Max_Side_Switches;
        public double leveling_bolt_mm_per_turn;
        public double outlier_adjust_threshold_mm;
        public bool use_toolhead_led;
        public string method;
        public int cool_temp_c;
        
        public class Positions{
          public AxesDouble front;
          public AxesDouble side;
          public AxesDouble @fixed;
        }
      }
      public class Lights{
        public LightSettings print;
        public LightSettings busy;
        public LightSettings chamber_ack;
        public LightSettings ready;
        public bool chamber_on;
        public string chamber_type;
        public LightSettings idle;
        public bool cycle_colors_on_start;
        public LightSettings tether;
        public bool knob_on;
        public LightSettings error;
        public LightSettings acknowledge;
        public bool invert_led_colors;
        public LightSettings priority_busy;

        public class LightSettings{
          public Settings knob;
          public Settings chamber;
          public int priority;

          public class Settings{
            public double r;
            public double g;
            public double b;
            public double period;
          }
        }
      }
      public class Materials{
        public Material im_pla;

        public class Material{
          public double linear_density;
          public int retract_rate;
          public double retract_distance;
          public Toolheads toolheads;
          public int temperature;
          public int restart_rate;
          public Acceleration acceleration;
          public double feed_diameter;
          public double ooze_feedstock_diameter;
          public double max_flow_rate;

        }
      }
      public class Toolheads {
        public Toolhead bronx;

      }
      public class GantryInformation{
        public int travel_speed_xy;
        public int max_fill_speed;
        public int max_inner_shell_speed;
        public int travel_speed_z;
        public int max_outer_shell_speed;
      }
      public class Acceleration {
        public int extruder_relax_decay_time;
        public ARate impulse_speed_limit_mm_per_s;
        public ARate max_speed_change_mm_per_s;
        public ARate rate_mm_per_s_sq;
        public int motor_damping_rshift;
        public int velocity_error_lshift;
        public int extruder_relax_shift_offset;
        public int extruder_responsiveness_rshift;
        public ARate min_speed_change_mm_per_s;
        public double[][] slip_compensation_table;
        public double feed_diameter;
        public double ooze_feedstock_distance;
        public double max_flow_rate;
        public bool allow_suspend_decel;
        public double split_move_distance_mm;
      }

    }
  }
  public class NetworkState{
    public string jsonrpc;
    public NetworkStateResult result;
    public int id;

    public class NetworkStateResult{
      public string gateway;
      public bool tethering;
      public string ip;
      public string wifi;
      public bool @static;
      public string[] dns;
      public string tether_name;
      public string state;
      public string netmask;
      public string service_hash;
      public string name;
    }
  }
}
