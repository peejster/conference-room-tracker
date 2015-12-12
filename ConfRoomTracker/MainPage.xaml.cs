using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Text;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ConfRoomTracker
{
    public sealed partial class MainPage : Page
    {
        static DeviceClient deviceClient;
        static string iotHubUri = "<enter your IoT Hub URI here>";
        static string deviceName = "<enter the registered device name here>";
        static string deviceKey = "<enter the device's device key here>";

        private const int ledPin = 5;
        private const int pirPin = 6;

        private GpioPin led;
        private GpioPin pir;

        private DispatcherTimer timer;

        public MainPage()
        {
            this.InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(60);
            timer.Tick += Timer_Tick;

            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceName, deviceKey), TransportType.Http1);

            InitGPIO();

            if (led != null)
            {
                timer.Start();
            }
        }

        private void InitGPIO()
        {
            // get the GPIO controller
            var gpio = GpioController.GetDefault();

            // return an error if there is no gpio controller
            if (gpio == null)
            {
                led = null;
                GpioStatus.Text = "There is no GPIO controller.";
                return;
            }

            // set up the LED on the defined GPIO pin
            // and set it to High to turn off the LED
            led = gpio.OpenPin(ledPin);
            led.Write(GpioPinValue.High);
            led.SetDriveMode(GpioPinDriveMode.Output);

            // set up the PIR sensor's signal on the defined GPIO pin
            // and set it's initial value to Low
            pir = gpio.OpenPin(pirPin);
            pir.SetDriveMode(GpioPinDriveMode.Input);

            GpioStatus.Text = "GPIO pins initialized correctly.";
        }

        private void Timer_Tick(object sender, object e)
        {
            TimeInterval.Text = DateTime.Now.ToString();

            // read the signal from the PIR sensor
            // if it is high, then motion was detected
            if (pir.Read() == GpioPinValue.High)
            {
                // turn on the LED
                led.Write(GpioPinValue.Low);

                // update the sensor status in the UI
                SensorStatus.Text = "Motion detected!";

                SendDeviceToCloudMessagesAsync("occupied");
            }
            else
            {
                // turn off the LED
                led.Write(GpioPinValue.High);

                // update the sensor status in the UI
                SensorStatus.Text = "No motion detected.";

                SendDeviceToCloudMessagesAsync("not occupied");
            }
        }

        private static async void SendDeviceToCloudMessagesAsync(string status)
        {
            var telemetryDataPoint = new
            {
                deviceId = deviceName,
                time = DateTime.Now.ToString(),
                roomStatus = status
            };
            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));

            await deviceClient.SendEventAsync(message);
        }
    }
}
