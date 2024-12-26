using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Collections.Generic;
using System.IO;
using Path = System.IO.Path;

namespace KakaoTalkBackupViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<MessageItem> Messages;
        private double currentScale = 1.0; // 현재 확대/축소 비율

        public MainWindow()
        {
            InitializeComponent();
            Messages = new List<MessageItem>();
        }

        private void OpenFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "텍스트 파일 열기"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadMessages(openFileDialog.FileName);
            }
        }

        private bool IsTimeFormattedLine(string line)
        {
            return line.Contains("년") && line.Contains("월") && line.Contains("일") && line.Contains(",");
        }

        private string CurrentFileDirectory { get; set; } // 텍스트 파일 디렉토리 경로

        private void LoadMessages(string filePath)
        {
            Messages.Clear();

            CurrentFileDirectory = Path.GetDirectoryName(filePath); // 텍스트 파일의 디렉토리 저장

            var lines = File.ReadAllLines(filePath);
            MessageItem currentMessage = null;

            foreach (var line in lines)
            {
                if (IsTimeFormattedLine(line))
                {
                    if (currentMessage != null)
                    {
                        Messages.Add(currentMessage);
                    }

                    var components = ParseMessageLine(line);
                    currentMessage = new MessageItem
                    {
                        Time = components.Time,
                        UserId = components.UserId,
                        Content = components.Content,
                        IsImage = components.IsImage,
                        ImagePath = components.ImagePath
                    };
                }
                else
                {
                    if (currentMessage != null)
                    {
                        currentMessage.Content += Environment.NewLine + line;
                    }
                }
            }

            if (currentMessage != null)
            {
                Messages.Add(currentMessage);
            }

            MessageListView.ItemsSource = Messages;
        }

        private (string Time, string UserId, string Content, bool IsImage, string ImagePath) ParseMessageLine(string line)
        {
            int firstComma = line.IndexOf(',');
            int secondColon = line.IndexOf(':', firstComma + 1);

            if (firstComma != -1 && secondColon != -1)
            {
                string time = line.Substring(0, firstComma).Trim();
                string userId = line.Substring(firstComma + 1, secondColon - firstComma - 1).Trim();
                string content = line.Substring(secondColon + 1).Trim();

                bool isImage = content.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                               content.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase);

                string imagePath = null;

                if (isImage)
                {
                    imagePath = Path.IsPathRooted(content)
                        ? content
                        : Path.Combine(CurrentFileDirectory, content);
                }

                return (time, userId, content, isImage, imagePath);
            }

            return (null, null, line, false, null);
        }

        private void MessageListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MessageListView.SelectedItem is MessageItem selectedMessage)
            {
                if (selectedMessage.IsImage)
                {
                    try
                    {
                        if (File.Exists(selectedMessage.ImagePath))
                        {
                            var imageUri = new Uri(Path.GetFullPath(selectedMessage.ImagePath), UriKind.Absolute);
                            ImageBox.Source = new BitmapImage(imageUri);
                            ResetZoom(); // 이미지를 변경할 때 확대/축소 상태 초기화
                        }
                        else
                        {
                            MessageBox.Show("이미지 파일을 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"이미지를 로드하는 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        ImageBox.Source = null;
                    }
                }
                else
                {
                    ImageBox.Source = null;
                }
            }
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            currentScale += 0.1;
            ImageScaleTransform.ScaleX = currentScale;
            ImageScaleTransform.ScaleY = currentScale;
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            currentScale = Math.Max(0.1, currentScale - 0.1); // 최소 배율 제한
            ImageScaleTransform.ScaleX = currentScale;
            ImageScaleTransform.ScaleY = currentScale;
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            ResetZoom();
        }

        private void ResetZoom()
        {
            currentScale = 1.0;
            ImageScaleTransform.ScaleX = currentScale;
            ImageScaleTransform.ScaleY = currentScale;
        }

        private class MessageItem
        {
            public string Time { get; set; }   // 대화 시간
            public string UserId { get; set; } // 사용자 아이디
            public string Content { get; set; } // 대화 내용
            public bool IsImage { get; set; } // 이미지 여부
            public string ImagePath { get; set; } // 이미지 경로
        }
    }
}
