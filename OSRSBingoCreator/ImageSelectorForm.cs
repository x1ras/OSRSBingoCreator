using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace OsrsBingoCreator
{
    public partial class ImageSelectorForm : Form
    {
        public string SelectedImagePath { get; private set; }
        public string SelectedImageUrl { get; private set; }
        private string currentPreviewPath = null;
        private string currentImageUrl = null;
        private const string WikiApiEndpoint = "https://oldschool.runescape.wiki/api.php";
        private const string ImgurUploadEndpoint = "https://api.imgur.com/3/image";
        private const string ImgurClientId = "6bb52baf522d42f";

        private static HttpClient InitializeHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            var httpClient = new HttpClient(handler);

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("OsrsBingoCreator/1.0 (Windows Forms App; Contact: UserRequest)");

            return httpClient;
        }

        private static readonly HttpClient client = InitializeHttpClient();

        public ImageSelectorForm()
        {
            InitializeComponent();
            SelectedImagePath = null;
            SelectedImageUrl = null;
            btnOK.Enabled = false;
        }

        private async Task<string> UploadImageToImgur(string imagePath)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                byte[] fileBytes = File.ReadAllBytes(imagePath);
                string base64Image = Convert.ToBase64String(fileBytes);

                var content = new MultipartFormDataContent();
                content.Add(new StringContent(base64Image), "image");
                
                using (var imgurClient = new HttpClient())
                {
                    imgurClient.DefaultRequestHeaders.Add("Authorization", $"Client-ID {ImgurClientId}");
                    
                    var response = await imgurClient.PostAsync(ImgurUploadEndpoint, content);
                    var responseJson = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"Imgur API Response: {responseJson}");
                    
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = JsonDocument.Parse(responseJson);
                    bool success = jsonResponse.RootElement.GetProperty("success").GetBoolean();

                    if (success)
                    {
                        string imageUrl = jsonResponse.RootElement.GetProperty("data").GetProperty("link").GetString();
                        return imageUrl;
                    }
                    else
                    {
                        throw new Exception("Upload failed: " + responseJson);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error uploading image to Imgur:\n{ex.Message}",
                    "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private async void btnSearchWiki_Click(object sender, EventArgs e)
        {
            string searchQuery = txtSearchQuery.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                MessageBox.Show("Please enter a search term.", "Search Empty", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string searchUrl = $"{WikiApiEndpoint}?action=query&generator=search&gsrsearch={Uri.EscapeDataString(searchQuery)}" +
                              "&gsrlimit=15&prop=pageimages&piprop=original&format=json&gsrnamespace=0";

            lstResults.Items.Clear();
            picPreview.Image = null;
            currentPreviewPath = null;
            currentImageUrl = null;
            btnOK.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                string jsonResponse = await client.GetStringAsync(searchUrl);
                JsonNode responseNode = JsonNode.Parse(jsonResponse);

                var pages = responseNode?["query"]?["pages"]?.AsObject();

                if (pages != null && pages.Count > 0)
                {
                    var pagesList = pages.Select(p => p.Value)
                                        .Where(page => page?["original"]?["source"] != null ||
                                                     page?["thumbnail"]?["source"] != null)
                                        .OrderBy(p => p?["index"]?.GetValue<int>())
                                        .ToList();

                    if (pagesList.Count > 0)
                    {
                        foreach (var page in pagesList)
                        {
                            string title = page?["title"]?.GetValue<string>();
                            if (!string.IsNullOrEmpty(title))
                            {
                                lstResults.Items.Add(title);
                            }
                        }
                    }
                    else
                    {
                        lstResults.Items.Add("No results with images found.");
                    }
                }
                else
                {
                    lstResults.Items.Add("No results found.");
                }
            }
            catch (HttpRequestException httpEx)
            {
                MessageBox.Show($"Network error searching wiki:\n{httpEx.Message}", "Wiki Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lstResults.Items.Add("Error searching.");
            }
            catch (JsonException jsonEx)
            {
                MessageBox.Show($"Error parsing wiki response:\n{jsonEx.Message}", "Wiki Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lstResults.Items.Add("Error parsing results.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred:\n{ex.Message}", "Wiki Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lstResults.Items.Add("Unexpected error.");
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void txtSearchQuery_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!string.IsNullOrWhiteSpace(txtSearchQuery.Text))
                {
                    btnSearchWiki.PerformClick();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }

        private async void lstResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstResults.SelectedItem == null || !(lstResults.SelectedItem is string selectedTitle) ||
               selectedTitle == "No results found." || selectedTitle.Contains("Error"))
            {
                return;
            }

            string imageUrlApi = $"{WikiApiEndpoint}?action=query&prop=pageimages&piprop=original&format=json&redirects=1&titles={Uri.EscapeDataString(selectedTitle)}";
            picPreview.Image?.Dispose();
            picPreview.Image = null;
            currentPreviewPath = null;
            currentImageUrl = null;
            btnOK.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                string jsonResponse = await client.GetStringAsync(imageUrlApi);
                JsonNode responseNode = JsonNode.Parse(jsonResponse);

                var pageEntry = responseNode?["query"]?["pages"]?.AsObject().FirstOrDefault();
                if (pageEntry == null || string.IsNullOrEmpty(pageEntry.Value.Key)) { throw new KeyNotFoundException($"..."); }
                string pageId = pageEntry.Value.Key; JsonNode pageData = pageEntry.Value.Value;
                string imageSourceUrl = pageData?["original"]?["source"]?.GetValue<string>() ?? pageData?["thumbnail"]?["source"]?.GetValue<string>();

                if (!string.IsNullOrEmpty(imageSourceUrl))
                {
                    string imageUrlForExtension = imageSourceUrl.Split('?')[0];
                    string extensionCheck = Path.GetExtension(imageUrlForExtension)?.ToLowerInvariant();

                    if (extensionCheck == ".gif")
                    {
                        MessageBox.Show(this,
                            "Image previews for GIF files are not currently supported.",
                            "Format Not Supported",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        picPreview.Image?.Dispose(); picPreview.Image = null; currentPreviewPath = null; currentImageUrl = null; btnOK.Enabled = false;
                        this.Cursor = Cursors.Default;
                        return;
                    }

                    byte[] imageBytes = await client.GetByteArrayAsync(imageSourceUrl);
                    if (imageBytes != null && imageBytes.Length > 0)
                    {
                        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        string cacheDir = Path.Combine(appDataPath, "OsrsBingoCreator", "WikiImageCache");
                        Directory.CreateDirectory(cacheDir);
                        string extension = extensionCheck ?? ".png";
                        if (!new[] { ".png", ".jpg", ".jpeg", ".bmp" }.Contains(extension)) { extension = ".png"; }
                        string safeFilename = $"{pageId}{extension}";
                        string localPath = Path.Combine(cacheDir, safeFilename);
                        TryWriteToFile(localPath, imageBytes);

                        picPreview.Image?.Dispose();
                        try
                        {
                            using (MemoryStream ms = new MemoryStream(imageBytes)) { picPreview.Image = new Bitmap(ms); }
                            currentPreviewPath = localPath;
                            currentImageUrl = imageSourceUrl;
                            btnOK.Enabled = true;
                        }
                        catch (System.Runtime.InteropServices.ExternalException) { MessageBox.Show(this, "Could not preview this image...", "Preview Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning); picPreview.Image = null; currentPreviewPath = null; currentImageUrl = null; btnOK.Enabled = false; }

                    }
                    else { throw new Exception("Downloaded image data was empty."); }
                }
                else
                {
                    picPreview.Image?.Dispose();
                    picPreview.Image = null;
                    currentPreviewPath = null;
                    currentImageUrl = null;
                    btnOK.Enabled = false;
                    MessageBox.Show(this,
                        "No image available for this page.",
                        "No Image",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is JsonException || ex is KeyNotFoundException || ex is IOException || ex is ArgumentException) { }
            catch (Exception) { }
            finally { this.Cursor = Cursors.Default; }
        }

        private async void btnBrowseLocal_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
                ofd.Title = "Select a Local Image";
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    string filePath = ofd.FileName;
                    try
                    {
                        this.Cursor = Cursors.WaitCursor;

                        string imageUrl = await UploadImageToImgur(filePath);

                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            using (var bmpTemp = new Bitmap(filePath))
                            {
                                picPreview.Image = new Bitmap(bmpTemp);
                            }

                            currentPreviewPath = filePath;
                            currentImageUrl = imageUrl;
                            btnOK.Enabled = true;
                        }
                        else
                        {
                            DialogResult result = MessageBox.Show(
                                "Failed to upload image to Imgur. Do you want to continue with local file only?\n" +
                                "(Note: The image won't be available when loading this board on other computers)",
                                "Upload Failed",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);

                            if (result == DialogResult.Yes)
                            {
                                using (var bmpTemp = new Bitmap(filePath))
                                {
                                    picPreview.Image = new Bitmap(bmpTemp);
                                }
                                currentPreviewPath = filePath;
                                currentImageUrl = null;
                                btnOK.Enabled = true;
                            }
                            else
                            {
                                picPreview.Image = null;
                                currentPreviewPath = null;
                                currentImageUrl = null;
                                btnOK.Enabled = false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error processing image:\n{ex.Message}", "Image Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        picPreview.Image = null;
                        currentPreviewPath = null;
                        currentImageUrl = null;
                        btnOK.Enabled = false;
                    }
                    finally
                    {
                        this.Cursor = Cursors.Default;
                    }
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentPreviewPath))
            {
                SelectedImagePath = currentPreviewPath;
                SelectedImageUrl = currentImageUrl;
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("No image selected or previewed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void ImageSelectorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            picPreview.Image?.Dispose();
        }

        private async void TestImgurUpload()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
                ofd.Title = "Select an Image to Test Upload";

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        string url = await UploadImageToImgur(ofd.FileName);
                        if (!string.IsNullOrEmpty(url))
                        {
                            MessageBox.Show($"Upload successful!\nURL: {url}", 
                                "Test Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Upload failed. Check network connection.", 
                                "Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}", "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private bool TryWriteToFile(string filePath, byte[] data, int retryCount = 3, int delayMs = 100)
        {
            string originalPath = filePath;
            string directory = Path.GetDirectoryName(filePath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            
            int counter = 1;
            while (File.Exists(filePath) && counter < 100)
            {
                filePath = Path.Combine(directory, $"{fileNameWithoutExt}_{counter}{extension}");
                counter++;
            }
            
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        fileStream.Write(data, 0, data.Length);
                    }
                    return true;
                }
                catch (IOException)
                {
                    if (i < retryCount - 1)
                    {
                        System.Threading.Thread.Sleep(delayMs);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return false;
        }
    }
}

namespace OSRSBingoCreator
{
    public class PictureBoxTag
    {
        public Point? Coordinates { get; set; }
        public string FilePath { get; set; }
        public string ImageUrl { get; set; }
    }
}