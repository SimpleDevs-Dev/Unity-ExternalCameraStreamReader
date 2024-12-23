using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Net;
using System.IO;
using System.Threading;
using System.Text;

public class CameraStreamReader : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField] private RawImage m_screenImage;

    [Header("=== Video Feed Stream Settings ===")]
    [SerializeField] private string m_url;
    [SerializeField] private int m_chunkSize = 640*480;

    private Texture2D m_texture;
    private WebResponse m_response;
    private Stream m_stream;
    private BinaryReader m_binaryReader;



    private bool m_streamActive = false;
    private readonly byte[] JpegHeader = new byte[] { 0xff, 0xd8 };

    // current encoded JPEG image
    public byte[] CurrentFrame { get; private set; }

    private void Start() {
        m_texture = new Texture2D(2,2);
        m_screenImage.texture = m_texture;
    }

    public async void StartListening() {
        // Initialize the request
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(m_url);
        // request.BeginGetResponse(OnGetResponse, request);

        // We must TRY a response. If it fails, we catch the response and adjust accordingly.
        try {
            // Wait until we get a response from the request
            m_response = await request.GetResponseAsync();

            // Because this is asynchronous, we may encounter a scenario where this script, somehow, is no longer enabled.
            // So we need to double-check that this is both active and enabled
            // If we are neither, then we must close our response and end cleanly.
            if(this && isActiveAndEnabled) {
                // Assuming success, then let's initialize a coroutine to start extracting frames
                StartCoroutine(GetFrames());
            }
            else m_response.Close();
        }
        catch (WebException e) {
            RestartListening(e);
        }
    }

    public void StopListening() {
        StopAllCoroutines();
        m_response?.Close();
        m_stream?.Close();
        m_binaryReader?.Close();
    }

    public void RestartListening(Exception exception) {
        // If we were provided an exception, we have to print out the error.
        if(exception != null) Debug.LogError($"[MJPEGClient] Restarting due to exception: {exception.Message}");
        
        // Force a stop
        StopListening();
        if(this && isActiveAndEnabled) StartListening();
    }

    private void OnDisable() {
        StopListening();
    }

    private void OnDestroy() {
        Destroy(m_texture);
    }

    private IEnumerator GetFrames() {
        // let's initialize a buffer for our image
        byte[] imageBuffer = new byte[1024 * 1024];

        // Initialize stream and binary reader
        m_stream = m_response.GetResponseStream();
        m_binaryReader = new BinaryReader(m_stream);

        // find our magic boundary value from our response
        string contentType = m_response.Headers["Content-Type"];
        string boundary = contentType.Split('=')[1].Replace("\"", "");
        byte[] boundaryBytes = Encoding.UTF8.GetBytes(boundary.StartsWith("--") ? boundary : "--" + boundary);

        // Also initialize the actual buffer where all data from the stream is dumped into
        byte[] buff = m_binaryReader.ReadBytes(m_chunkSize);

        // Initialize while loop
        while(true) {
            // find the JPEG header. note that JPEG headers start with `0xff` and `0xd8` in this order. 
            int imageStart = FindBytes(buff, JpegHeader);
            
            // Potential fail case: we didn't find JPEG header. In that case, we must restart the listener
            if (imageStart == -1) {
                RestartListening(new Exception("Unable to find JPEG data length!"));
                yield break;
            }
            
            // Here, the JPEG headers are found! Then we need to wait until we get the end of the JPEG.
            
            // Copy the start of the JPEG image to the imageBuffer
            int size = buff.Length - imageStart;
            Array.Copy(buff, imageStart, imageBuffer, 0, size);

            // Again, we must wait until we get the end of the JPEG.
            // We process data from the server as chunks of data. So the JPEG ending can be any point inside of the chunk.
            // So we need to use a while loop and wait... until we find the JPEG heaer in whichever chunk we process next.
            while (true) {
                // Read from binary reader
                buff = m_binaryReader.ReadBytes(m_chunkSize);

                // Find the end of the jpeg
                int imageEnd = FindBytes(buff, boundaryBytes);
                
                // If we DID find the end, then we can simply copy the data from the buffer into the image buffer we initialized at the start of the coroutine.
                if (imageEnd != -1) {
                    
                    // copy the remainder of the JPEG to the imageBuffer
                    Array.Copy(buff, 0, imageBuffer, size, imageEnd);
                    size += imageEnd;

                    // Copy the latest frame into image buffer, then update our texture
                    byte[] frame = new byte[size];
                    Array.Copy(imageBuffer, 0, frame, 0, size);
                    m_texture.LoadImage(imageBuffer);
                    yield return null;

                    // Copy the leftover data to the start
                    Array.Copy(buff, imageEnd, buff, 0, buff.Length - imageEnd);

                    // fill the remainder of the buffer with new data and start over
                    byte[] temp = m_binaryReader.ReadBytes(imageEnd);
                    Array.Copy(temp, 0, buff, buff.Length - imageEnd, temp.Length);
                    break;
                }

                // copy all of the data to the imageBuffer
                Array.Copy(buff, 0, imageBuffer, size, buff.Length);
                size += buff.Length;
                yield return null;
            }
        }

    }

    private void OnGetResponse(IAsyncResult asyncResult) {
        Debug.Log("OnGetResponse");
        byte[] imageBuffer = new byte[1024 * 1024];

        Debug.Log("Starting request");
        HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;

        try {
            Debug.Log("OnGetResponse try entered.");
            HttpWebResponse resp = (HttpWebResponse)request.EndGetResponse(asyncResult);
            Debug.Log("response received");

            // find our magic boundary value
            string contentType = resp.Headers["Content-Type"];
            Debug.Log(contentType);

            string boundary = resp.Headers["Content-Type"].Split('=')[1].Replace("\"", "");
            byte[] boundaryBytes = Encoding.UTF8.GetBytes(boundary.StartsWith("--") ? boundary : "--" + boundary);
            Debug.Log(boundary);

            Stream stream = resp.GetResponseStream();
            BinaryReader binaryReader = new BinaryReader(stream);

            m_streamActive = true;
            byte[] buff = binaryReader.ReadBytes(m_chunkSize);

            while(m_streamActive) {
                // find the JPEG header
                int imageStart = FindBytes(buff, JpegHeader);

                if (imageStart != -1) { 
                    Debug.Log("JPEG header found!");

                    // copy the start of the JPEG image to the imageBuffer
                    int size = buff.Length - imageStart;
                    Array.Copy(buff, imageStart, imageBuffer, 0, size);

                    while (true) {
                        buff = binaryReader.ReadBytes(m_chunkSize);

                        // Find the end of the jpeg
                        int imageEnd = FindBytes(buff, boundaryBytes);
                        if (imageEnd != -1) {
                            // copy the remainder of the JPEG to the imageBuffer
                            Array.Copy(buff, 0, imageBuffer, size, imageEnd);
                            size += imageEnd;

                            // Copy the latest frame into `CurrentFrame`
                            byte[] frame = new byte[size];
                            Array.Copy(imageBuffer, 0, frame, 0, size);
                            CurrentFrame = frame;

                            // tell whoever's listening that we have a frame to draw
                            UpdateScreen(CurrentFrame);

                            // copy the leftover data to the start
                            Array.Copy(buff, imageEnd, buff, 0, buff.Length - imageEnd);

                            // fill the remainder of the buffer with new data and start over
                            byte[] temp = binaryReader.ReadBytes(imageEnd);

                            Array.Copy(temp, 0, buff, buff.Length - imageEnd, temp.Length);
                            break;
                        }

                        // copy all of the data to the imageBuffer
                        Array.Copy(buff, 0, imageBuffer, size, buff.Length);
                        size += buff.Length;

                        if (!m_streamActive) {
                            Debug.Log("CLOSING");
                            resp.Close();
                            break;
                        }
                    }

                } else {
                    Debug.Log("JPEG header not found...");
                }
            }
        }
        catch (Exception ex) {
            Debug.LogError(ex.Message);
            return;
        }
    }

    private void UpdateScreen(byte[] imageData) {
        Texture2D newTexture = new Texture2D(2,2);
        newTexture.LoadImage(imageData);
        m_screenImage.texture = newTexture;
    }

    public static int FindBytes(byte[] buff, byte[] search) {
        // enumerate the buffer but don't overstep the bounds
        for (int start = 0; start < buff.Length - search.Length; start++) {
            // we found the first character
            if (buff[start] == search[0]) {
                int next;

                // traverse the rest of the bytes
                for (next = 1; next < search.Length; next++) {
                    // if we don't match, bail
                    if (buff[start + next] != search[next]) break;
                }

                if (next == search.Length) return start;
            }
        }
        // not found
        return -1;
    }
}
