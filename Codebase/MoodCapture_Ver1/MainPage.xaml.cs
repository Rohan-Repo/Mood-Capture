using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


// For Camera Functions
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

// For Project Oxford Setup
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using System.Text;
using Windows.Storage.Pickers;
using System.Net.Http;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MoodCapture_Ver1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Camera Stuff
        CameraCaptureUI cameraCaptureUI = new CameraCaptureUI();
        StorageFile photo = null;
        IRandomAccessStream imageStream = null;

        // Emotion API Stuff
        const string APIKEY = "b618eb71c5254c3094bbf8f5af407e3e";
        EmotionServiceClient emotionServiceClient = new EmotionServiceClient(APIKEY);
        // Returns Emotion for every face in the array : Returns JSON Format
        Emotion[] emotionResult = null;

        public MainPage()
        {
            this.InitializeComponent();

            // Image Format 
            cameraCaptureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;

            // Allowing Cropping
            cameraCaptureUI.PhotoSettings.CroppedSizeInPixels = new Size(200, 200);
        }

        private async void btnCapture_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                txtEmotionInfo.Text = "";
                txtFinalEmotion.Text = "";

                photo = await cameraCaptureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

                showImage(photo);
                
            }
            catch
            {
                txtEmotionInfo.Text = "An Error in Capturing the Photo";
            }
        }

        private async void showImage( StorageFile pic )
        {
            if (pic == null)
            {
                // User Cancelled the Photo
                return;
            }
            else
            {
                imageStream = await pic.OpenAsync(FileAccessMode.Read);

                // Everything after this was done just to Show the Image correctly
                // Convert Stream to Bitmap
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(imageStream);
                // Store the Bitmap
                SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                // For better Display : Alpha - Transperancy
                SoftwareBitmap softwareBitmapBGR8 = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                // Bitmap Source
                SoftwareBitmapSource softwareBitmapSource = new SoftwareBitmapSource();
                await softwareBitmapSource.SetBitmapAsync(softwareBitmapBGR8);

                // Setting the ImageBox's Source to our formatted BitmapSource
                imgFace.Source = softwareBitmapSource;

                btnCheckEmotion.IsEnabled = true;
            }
        }

        private async void btnCheckEmotion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtFinalEmotion.Text = "";
                // txtEmotionInfo.Text = "";

                //if( txtEmotionInfo.Text != null || txtFinalEmotion.Text != null )
                //{
                //    txtFinalEmotion.Text = "Please Click Another Photograph";
                //    txtEmotionInfo.Text = "";
                //    imgFace.Source = null;
                //    return;
                //}

                if (imageStream == null)
                {
                    txtFinalEmotion.Text = "Please Click Another Photograph";
                    txtEmotionInfo.Text = "";
                    imgFace.Source = null;
                    return;
                }

                // Raw Image Stream as API needs a raw Image Stream
                emotionResult = await emotionServiceClient.RecognizeAsync(imageStream.AsStream());

                if (emotionResult != null)
                {
                    // emotionResult will contain info. of all the faces & their Emotion 
                    // But we need only 1 Image so we select emotionResult[0]

                    // Scores contain all the different Emotions like anger, sadness etc.

                    if (emotionResult.Length == 0)
                    {
                        txtFinalEmotion.Text = "NO Emotions Returned :(";
                        btnCheckEmotion.IsEnabled = false;
                    }
                        


                    Scores arrEmotion = emotionResult[0].Scores;
                    StringBuilder stringBuilder = new StringBuilder(" Your Emotions Are : \n ");

                    stringBuilder.Append("Happiness : " + arrEmotion.Happiness + "\n");
                    stringBuilder.Append("Anger : " + arrEmotion.Anger + "\n");
                    stringBuilder.Append("Contempt : " + arrEmotion.Contempt + "\n");
                    stringBuilder.Append("Disgust : " + arrEmotion.Disgust + "\n");
                    stringBuilder.Append("Fear : " + arrEmotion.Fear + "\n");
                    stringBuilder.Append("Surprise : " + arrEmotion.Surprise + "\n");
                    stringBuilder.Append("Neutral : " + arrEmotion.Neutral + "\n");
                    stringBuilder.Append("Sadness : " + arrEmotion.Sadness + "\n");
                    
                    txtEmotionInfo.Text = stringBuilder.ToString();

                    StringBuilder stringBuilder2 = new StringBuilder("The Emotion you are feeling is : ");

                    if (arrEmotion.Happiness >= 0.4)
                        stringBuilder2.Append(" Happy ");
                    if (arrEmotion.Anger >= 0.4)
                        stringBuilder2.Append(" Angry ");
                    if (arrEmotion.Contempt >= 0.4)
                        stringBuilder2.Append(" Contempt ");
                    if (arrEmotion.Disgust >= 0.4)
                        stringBuilder2.Append(" Disgust ");
                    if (arrEmotion.Fear >= 0.4)
                        stringBuilder2.Append(" Scared ");
                    if (arrEmotion.Surprise >= 0.4)
                        stringBuilder2.Append(" Surprised ");
                    if (arrEmotion.Neutral >= 0.4)
                        stringBuilder2.Append(" Neutral ");
                    if (arrEmotion.Sadness >= 0.4)
                        stringBuilder2.Append(" Sad ");

                    txtFinalEmotion.Text = stringBuilder2.ToString();
                    // imgFace.Source = null;
                    imageStream = null;
                    photo = null;
                    btnCheckEmotion.IsEnabled = false;

                }
            }
            catch( HttpRequestException e2 )
            {
                txtFinalEmotion.Text = "Issue with Establishing a Connection with the API";
                txtEmotionInfo.Text = "Error Returning the Emotion";
            }
            catch (Exception e1)
            {                
                txtEmotionInfo.Text = "Error Returning the Emotion";                
            }
        }

        private async void btnSelectFromFile_Click(object sender, RoutedEventArgs e)
        {
            txtEmotionInfo.Text = "";
            txtFinalEmotion.Text = "";

            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.ViewMode = PickerViewMode.Thumbnail;
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;            
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            fileOpenPicker.FileTypeFilter.Add(".jpeg");
            fileOpenPicker.FileTypeFilter.Add(".png");

            photo = await fileOpenPicker.PickSingleFileAsync();

            showImage( photo );
            
        }
        
    }
}
