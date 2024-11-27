using System.Windows;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using System.ComponentModel;

namespace HalfQwertyKeyBoard
{
    public partial class MainWindow : Window
    {
        [STAThread]

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookCallbackDelegate lpfn, IntPtr wParam, uint lParam);

        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private static int WH_KEYBOARD_LL = 13;
        private static int WM_KEYDOWN = 0x100; //256
        //private static int WM_KEYUP = 0x101; //257
        private static bool ishmode = false;
        private static int trigger = 32; //kanji: 25, rctrl: 163, kana: 21
        private static bool issentkey = false;
        private bool isoff = true;

        private static HookCallbackDelegate hcDelegate;

        private static IntPtr hook;

        private static Dictionary<Keys, VirtualKeyCode> keybinds = new Dictionary<Keys, VirtualKeyCode>
        {
            {Keys.A, VirtualKeyCode.VK_A},
            {Keys.B, VirtualKeyCode.VK_B},
            {Keys.C, VirtualKeyCode.VK_C},
            {Keys.D, VirtualKeyCode.VK_D},
            {Keys.E, VirtualKeyCode.VK_E},
            {Keys.F, VirtualKeyCode.VK_F},
            {Keys.G, VirtualKeyCode.VK_G},
            {Keys.H, VirtualKeyCode.VK_G}, // Map H to G
            {Keys.I, VirtualKeyCode.VK_E}, // Map I to E
            {Keys.J, VirtualKeyCode.VK_F}, // Map J to F
            {Keys.K, VirtualKeyCode.VK_D}, // Map K to D
            {Keys.L, VirtualKeyCode.VK_S}, // Map L to S
            {Keys.M, VirtualKeyCode.VK_V}, // Map M to V
            {Keys.N, VirtualKeyCode.VK_B},
            {Keys.O, VirtualKeyCode.VK_W}, // Map O to W
            {Keys.P, VirtualKeyCode.VK_Q}, // Map P to Q
            {Keys.Q, VirtualKeyCode.VK_Q},
            {Keys.R, VirtualKeyCode.VK_R},
            {Keys.S, VirtualKeyCode.VK_S},
            {Keys.T, VirtualKeyCode.VK_T},
            {Keys.U, VirtualKeyCode.VK_R}, // Map U to R
            {Keys.V, VirtualKeyCode.VK_V},
            {Keys.W, VirtualKeyCode.VK_W},
            {Keys.X, VirtualKeyCode.VK_X},
            {Keys.Y, VirtualKeyCode.VK_T}, // Map Y to T
            {Keys.Z, VirtualKeyCode.VK_Z},
            {Keys.OemSemicolon, VirtualKeyCode.VK_A}, // Map ; to A
            {Keys.Oemcomma, VirtualKeyCode.VK_C}, // Map , to C
            {Keys.OemPeriod, VirtualKeyCode.VK_X}, // Map . to X
            {Keys.Oem2, VirtualKeyCode.VK_Z}, // Map / to Z
            {Keys.D1, VirtualKeyCode.VK_1},
            {Keys.D2, VirtualKeyCode.VK_2},
            {Keys.D3, VirtualKeyCode.VK_3},
            {Keys.D4, VirtualKeyCode.VK_4},
            {Keys.D5, VirtualKeyCode.VK_5},
            {Keys.D6, VirtualKeyCode.VK_5}, // Map 6 to 5
            {Keys.D7, VirtualKeyCode.VK_4}, // Map 7 to 4
            {Keys.D8, VirtualKeyCode.VK_3}, // Map 8 to 3
            {Keys.D9, VirtualKeyCode.VK_2}, // Map 9 to 2
            {Keys.D0, VirtualKeyCode.VK_1},  // Map 0 to 1
        };
        private static Dictionary<Keys, VirtualKeyCode> specialkeys = new Dictionary<Keys, VirtualKeyCode>
        {
            {Keys.Enter, VirtualKeyCode.SPACE },
            {Keys.LShiftKey, VirtualKeyCode.RETURN }
        };

        Binder binder = new();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = binder;

            hcDelegate = HookCallback;

            System.Windows.Application.Current.Exit += (sender, e) => UnhookWindowsHook(hook);
        }

        private void btn_trgr_Click(object sender, RoutedEventArgs e)
        {
            if (isoff) //turn off
            {
                UnhookWindowsHook(hook);

                binder.HStateData = "Off";
                binder.StateData = "Turned off(Unhooked)";
            }
            else //turn on
            {
                string mainModuleName = Process.GetCurrentProcess().MainModule.ModuleName;
                hook = SetWindowsHookEx(WH_KEYBOARD_LL, hcDelegate, GetModuleHandle(mainModuleName), 0);

                binder.HStateData = "On";
                binder.StateData = "Normal mode (Hooked)";
            }

            isoff = !isoff;
        }

        public IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) //lctrl or kanjimode for ime
        {
            InputSimulator isim = new();
            IntPtr ret = CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);

            if (nCode < 0 || wParam != WM_KEYDOWN)
            {
                return ret;
            }

            int vkCode = Marshal.ReadInt32(lParam);

            if (issentkey)
            {
                return ret;
            }

            if (vkCode == trigger)
            {
                ishmode = !ishmode;

                if (ishmode)
                { binder.StateData = "HalfQwerty mode (Hooked)"; }
                else
                { binder.StateData = "Normal mode (Hooked)"; }

                return (IntPtr)1;
            }

            if (specialkeys.TryGetValue((Keys)vkCode, out VirtualKeyCode newkey))
            {
                issentkey = true;
                isim.Keyboard.KeyPress(newkey);
                issentkey = false;
                return (IntPtr)1;
            }

            if (ishmode && keybinds.TryGetValue((Keys)vkCode, out VirtualKeyCode newKey))
            {
                issentkey = true;
                isim.Keyboard.KeyPress(newKey);
                issentkey = false;
                return (IntPtr)1;
            }

            return ret;
        }

        public delegate IntPtr HookCallbackDelegate(int nCode, IntPtr wParam, IntPtr lParam);

        private void UnhookWindowsHook(IntPtr hook)
        {
            if (hook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hook);
                hook = IntPtr.Zero;
            }
        }
    }

    class Binder : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private string statedata = "Turned off(Unhooked)";
        private string hstatedata = "Off";

        public string StateData
        {
            get { return statedata; }
            set
            {
                statedata = value;
                OnproprtyChanged("StateData");
            }
        }

        public string HStateData
        {
            get { return hstatedata; }
            set 
            {
                hstatedata = value;
                OnproprtyChanged("HStateData");
            }
        }

        public void OnproprtyChanged(string proprtyname)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(proprtyname));
        }
    }
}