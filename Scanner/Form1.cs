using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vintasoft.Twain;

namespace Scanner
{
    public partial class Form1 : Form
    {

        #region Fields

        /// <summary>
        /// TWAIN device manager.
        /// </summary>
        DeviceManager _deviceManager;

        /// <summary>
        /// Current device.
        /// </summary>
        Device _currentDevice;

        /// <summary>
        /// Indicates that device is acquiring image(s).
        /// </summary>
        bool _isImageAcquiring;

        /// <summary>
        /// Acquired image collection.
        /// </summary>
        AcquiredImageCollection _images = new AcquiredImageCollection();

        /// <summary>
        /// Current image index in acquired image collection.
        /// </summary>
        int _imageIndex = -1;

        /// <summary>
        /// Determines that image acquistion must be canceled because application's form is closing.
        /// </summary>
        bool _cancelTransferBecauseFormIsClosing;

        #endregion
        public Form1()
        {
            InitializeComponent();

            // get country and language for TWAIN device manager
            CountryCode country;
            LanguageType language;
            GetCountryAndLanguage(out country, out language);

            // create TWAIN device manager
            _deviceManager = new DeviceManager(this, this.Handle, country, language);
        }

        /// <summary>
        /// Starts the image acquisition.
        /// </summary>
        private void ScannerButton_Click(object sender, EventArgs e)
        {
            //adquirir imágenes
            AcquireImage(false, PixelType.BW, TransferMode.Native, 150);
        }


        #region Scan process
        private void AcquireImage(bool showDefaultDeviceSelectionDialog,
            PixelType pixelType, TransferMode transferMode, float resolution, bool duplexEnabled = true)
        {

            // specify that image acquisition is started
            _isImageAcquiring = true;

            try
            {


                //Opens the TWAIN device manager
                OpenDevice();

                // if no devices are found in the system
                if (_deviceManager.Devices.Count == 0)
                {
                    throw new ApplicationException("No se encuentran los dispositivos");
     
                }

                // select the device
                if (showDefaultDeviceSelectionDialog)
                    _deviceManager.ShowDefaultDeviceSelectionDialog();


                if (_currentDevice != null)
                    // unsubscribe from the device events
                    UnsubscribeFromDeviceEvents(_currentDevice);


                // get reference to the current device
                Device device = _deviceManager.DefaultDevice;

                if (device == null)
                {
                    // specify that image acquisition is finished
                    _isImageAcquiring = false;

                    MessageBox.Show("No se encontró el dispositivo", "Dispositivo TWAIN", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _currentDevice = device;
                // subscribe to the device events
                SubscribeToDeviceEvents(_currentDevice);

                // subscribe to the device events
                SubscribeToDeviceEvents(_currentDevice);

                // set the image acquisition parameters
                device.ShowUI = false;
                device.ModalUI = false;
                device.ShowIndicators = false;
                device.DisableAfterAcquire = true;
                device.TransferMode = transferMode;

                try
                {
                    // open the device
                    device.Open();
                }
                catch (TwainException ex)
                {
                    // specify that image acquisition is finished
                    _isImageAcquiring = false;

                    MessageBox.Show(GetFullExceptionMessage(ex), "Dispositivo TWAIN", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }


                // set device capabilities

                // pixel type
                try
                {
                    device.PixelType = pixelType;
                }
                catch (TwainException)
                {
                    MessageBox.Show($"Pixel type '{pixelType}' no soportado.", "Dispositivo TWAIN");
                }

                // unit of measure
                try
                {
                    if (device.UnitOfMeasure != UnitOfMeasure.Inches)
                        device.UnitOfMeasure = UnitOfMeasure.Inches;
                }
                catch (TwainException)
                {
                    MessageBox.Show("Unit of measure 'Inches' is not supported.", "TWAIN device");
                }

                // resolution
                Resolution _resolution = new Resolution(resolution, resolution);
                try
                {
                    device.Resolution = _resolution;
                }
                catch (TwainException)
                {
                }

                // if device is Fujitsu scanner
                if (device.Info.ProductName.ToUpper().StartsWith("FUJITSU"))
                {
                    DeviceCapability undefinedImageSizeCap = device.Capabilities.Find(DeviceCapabilityId.IUndefinedImageSize);
                    // if undefined image size is supported
                    if (undefinedImageSizeCap != null)
                    {
                        try
                        {
                            // enable undefined image size feature
                            undefinedImageSizeCap.SetValue(true);
                        }
                        catch (TwainDeviceCapabilityException)
                        {
                        }
                    }
                }

                try
                {
                    // if ADF present
                    if (!device.Info.IsWIA && device.HasFeeder)
                    {
                        // enable/disable ADF if necessary
                        try
                        {
                            device.DocumentFeeder.Enabled =true;
                        }
                        catch (TwainDeviceCapabilityException)
                        {
                        }

                        // enable/disable duplex if necessary
                        try
                        {

                            device.DocumentFeeder.DuplexEnabled = duplexEnabled;
                        }
                        catch (TwainDeviceCapabilityException)
                        {
                        }
                    }

                }
                catch (TwainException ex)
                {
                    MessageBox.Show(GetFullExceptionMessage(ex), "Dispositivo TWAIN");
                }

                // if device supports asynchronous events
                if (device.IsAsyncEventsSupported)
                {
                    try
                    {
                        // enable all asynchronous events supported by device
                        device.AsyncEvents = device.GetSupportedAsyncEvents();
                    }
                    catch
                    {
                    }
                }


                try
                {
                    // start image acquition process
                    device.Acquire();
                }
                catch (TwainException ex)
                {
                    // specify that image acquisition is finished
                    _isImageAcquiring = false;

                    MessageBox.Show(GetFullExceptionMessage(ex), "Dispositivo TWAIN", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            finally
            {
                //update UI
            }

        }

        /// <summary>
        /// Opens the TWAIN device manager.
        /// </summary>
        private void OpenDevice()
        {

            try
            {
                // if device manager is open - close the device manager
                if (_deviceManager.State != DeviceManagerState.Opened)
                {
                    // try to use TWAIN device manager 2.x
                    _deviceManager.IsTwain2Compatible = true;
                    // if TWAIN device manager 2.x is not available
                    if (!_deviceManager.IsTwainAvailable)
                    {
                        // try to use TWAIN device manager 1.x
                        _deviceManager.IsTwain2Compatible = false;
                        // if TWAIN device manager 1.x is not available
                        if (!_deviceManager.IsTwainAvailable)
                        {
                            // show dialog with error message
                            MessageBox.Show("El administrador de dispositivos TWAIN no está disponible", "TWAIN device manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    // if 64-bit TWAIN2 device manager is used
                    if (IntPtr.Size == 8 && _deviceManager.IsTwain2Compatible)
                    {
                        _deviceManager.Use32BitDevices();
                    }
                    else
                    {
                        _deviceManager.Use64BitDevices();
                    }

                    // open the device manager
                    _deviceManager.Open();
                }
            }
            catch (Exception ex)
            {
                // close the device manager
                if (_deviceManager.State == DeviceManagerState.Opened)
                    _deviceManager.Close();
                // show dialog with error message
                MessageBox.Show(GetFullExceptionMessage(ex), "Administración de dispositivos TWAIN", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        #endregion


        #region Eventos

        /// <summary>
        /// Subscribe to the device events.
        /// </summary>
        private void SubscribeToDeviceEvents(Device device)
        {
            device.ImageAcquiringProgress += new EventHandler<ImageAcquiringProgressEventArgs>(device_ImageAcquiringProgress);
            device.ImageAcquired += new EventHandler<ImageAcquiredEventArgs>(device_ImageAcquired);
            device.ScanFailed += new EventHandler<ScanFailedEventArgs>(device_ScanFailed);
            device.AsyncEvent += new EventHandler<DeviceAsyncEventArgs>(device_AsyncEvent);
            device.ScanFinished += new EventHandler(device_ScanFinished);
        }

        /// <summary>
        /// Unsubscribe from the device events.
        /// </summary>
        private void UnsubscribeFromDeviceEvents(Device device)
        {
            device.ImageAcquiringProgress -= new EventHandler<ImageAcquiringProgressEventArgs>(device_ImageAcquiringProgress);
            device.ImageAcquired -= new EventHandler<ImageAcquiredEventArgs>(device_ImageAcquired);
            device.ScanFailed -= new EventHandler<ScanFailedEventArgs>(device_ScanFailed);
            device.AsyncEvent -= new EventHandler<DeviceAsyncEventArgs>(device_AsyncEvent);
            device.ScanFinished -= new EventHandler(device_ScanFinished);
        }

        /// <summary>
        /// Image acquiring progress is changed.
        /// </summary>
        private void device_ImageAcquiringProgress(object sender, ImageAcquiringProgressEventArgs e)
        {
            // image acquistion must be canceled because application's form is closing
            if (_cancelTransferBecauseFormIsClosing)
            {
                // cancel image acquisition
                _currentDevice.CancelTransfer();
                return;
            }

            //imageAcquisitionProgressBar.Value = (int)e.Progress;

            //if (imageAcquisitionProgressBar.Value == 100)
            //{
            //    imageAcquisitionProgressBar.Value = 0;
            //}
        }

        /// <summary>
        /// Image is acquired.
        /// </summary>
        private void device_ImageAcquired(object sender, ImageAcquiredEventArgs e)
        {
            // image acquistion must be canceled because application's form is closing
            if (_cancelTransferBecauseFormIsClosing)
            {
                // cancel image acquisition
                _currentDevice.CancelTransfer();
                return;
            }

            _images.Add(e.Image);

            //SetCurrentImage(_images.Count - 1);
        }

        /// <summary>
        /// Scan is failed.
        /// </summary>
        private void device_ScanFailed(object sender, ScanFailedEventArgs e)
        {
            // show error message
            MessageBox.Show(e.ErrorString, "Error de escaneo", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// An asynchronous event was generated by device.
        /// </summary>
        private void device_AsyncEvent(object sender, DeviceAsyncEventArgs e)
        {
            switch (e.DeviceEvent)
            {
                case DeviceEventId.PaperJam:
                    MessageBox.Show("El papel está atascado.");
                    break;

                case DeviceEventId.CheckDeviceOnline:
                    MessageBox.Show("Verifique que el dispositivo esté en línea.");
                    break;

                case DeviceEventId.CheckBattery:
                    MessageBox.Show(string.Format("DeviceEvent: Device={0}, Event={1}, BatteryMinutes={2}, BatteryPercentage={3}",
                        e.DeviceName, e.DeviceEvent, e.BatteryMinutes, e.BatteryPercentage));
                    break;

                case DeviceEventId.CheckPowerSupply:
                    MessageBox.Show(string.Format("DeviceEvent: Device={0}, Event={1}, PowerSupply={2}",
                        e.DeviceName, e.DeviceEvent, e.PowerSupply));
                    break;

                case DeviceEventId.CheckResolution:
                    MessageBox.Show(string.Format("DeviceEvent: Device={0}, Event={1}, Resolution={2}",
                        e.DeviceName, e.DeviceEvent, e.Resolution));
                    break;

                case DeviceEventId.CheckFlash:
                    MessageBox.Show(string.Format("DeviceEvent: Device={0}, Event={1}, FlashUsed={2}",
                        e.DeviceName, e.DeviceEvent, e.FlashUsed));
                    break;

                case DeviceEventId.CheckAutomaticCapture:
                    MessageBox.Show(string.Format("DeviceEvent: Device={0}, Event={1}, AutomaticCapture={2}, TimeBeforeFirstCapture={3}, TimeBetweenCaptures={4}",
                        e.DeviceName, e.DeviceEvent, e.AutomaticCapture, e.TimeBeforeFirstCapture, e.TimeBetweenCaptures));
                    break;

                default:
                    MessageBox.Show(string.Format("DeviceEvent: Device={0}, Event={1}",
                        e.DeviceName, e.DeviceEvent));
                    break;
            }

            // if device is enabled or transferring images
            if (_currentDevice.State >= DeviceState.Enabled)
                return;

            // close the device
            _currentDevice.Close();
        }

        /// <summary>
        /// Scan is finished.
        /// </summary>
        private void device_ScanFinished(object sender, EventArgs e)
        {
            // close the device
            _currentDevice.Close();

            // specify that image acquisition is finished
            _isImageAcquiring = false;

            // update UI
            //UpdateUI();
        }


        #endregion

        #region Método genéricos

        /// <summary>
        /// Returns country and language for TWAIN device manager.
        /// </summary>
        /// <remarks>
        /// Unfortunately only KODAK scanners allow to set country and language.
        /// </remarks>
        private void GetCountryAndLanguage(out CountryCode country, out LanguageType language)
        {
            country = CountryCode.Usa;
            language = LanguageType.EnglishUsa;

            switch (CultureInfo.CurrentUICulture.Parent.IetfLanguageTag)
            {
                case "de":
                    country = CountryCode.Germany;
                    language = LanguageType.German;
                    break;

                case "es":
                    country = CountryCode.Spain;
                    language = LanguageType.Spanish;
                    break;

                case "fr":
                    country = CountryCode.France;
                    language = LanguageType.French;
                    break;

                case "it":
                    country = CountryCode.Italy;
                    language = LanguageType.Italian;
                    break;

                case "pt":
                    country = CountryCode.Portugal;
                    language = LanguageType.Portuguese;
                    break;

                case "ru":
                    country = CountryCode.Russia;
                    language = LanguageType.Russian;
                    break;
            }
        }


        /// <summary>
        /// Returns the message of exception and inner exceptions.
        /// </summary>
        private string GetFullExceptionMessage(Exception ex)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(ex.Message);

            Exception innerException = ex.InnerException;
            while (innerException != null)
            {
                if (ex.Message != innerException.Message)
                    sb.AppendLine(string.Format("Inner exception: {0}", innerException.Message));
                innerException = innerException.InnerException;
            }

            return sb.ToString();
        }

        #endregion
    }
}
