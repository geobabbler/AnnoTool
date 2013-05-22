<%@ WebHandler Language="C#" Class="ProxyHandler" %>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


public class ProxyHandler : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        try
        {
            string uri = context.Request.RawUrl.Substring(context.Request.RawUrl.IndexOf("?") + 1);
            uri = HttpContext.Current.Server.UrlDecode(uri);
            if (uri.StartsWith("ping"))
            {
                context.Response.Write("<html><body>Hello ProxyHandler</body></html>");
                return;
            }

            System.Net.WebRequest req = System.Net.WebRequest.Create(new Uri(uri));
            req.Method = context.Request.HttpMethod;
            //req.Timeout = 15000;

            // Set body of request for POST requests
            if (context.Request.InputStream.Length > 0)
            {
                byte[] bytes = new byte[context.Request.InputStream.Length];
                context.Request.InputStream.Read(bytes, 0, (int)context.Request.InputStream.Length);
                req.ContentLength = bytes.Length;
                req.ContentType = "application/x-www-form-urlencoded";
                System.IO.Stream outputStream = req.GetRequestStream();
                outputStream.Write(bytes, 0, bytes.Length);
                outputStream.Close();
            }

            System.Net.WebResponse res = req.GetResponse();
            context.Response.ContentType = res.ContentType;

            // Text responses
            if (res.ContentType.Contains("text") || res.ContentType.Contains("application/vnd.ogc"))
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(res.GetResponseStream());
                string strResponse = sr.ReadToEnd();
                context.Response.Write(strResponse);
            }
            // Convert all png images to 32-bit for full alpha (transparency support) in Silverlight    
            else if (res.ContentType.Contains("/png"))
            {
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(res.GetResponseStream());
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                context.Response.BinaryWrite(ms.ToArray());

            }
            else
            {

                // Binary responses (non-png images)            
                byte[] b;
                System.IO.Stream s = res.GetResponseStream();
                System.IO.MemoryStream mem = new System.IO.MemoryStream();
                CopyStream(s, mem);
                b = mem.GetBuffer();
                byte[] truncated = mem.ToArray();
                //using (System.IO.BinaryReader br = new System.IO.BinaryReader(res.GetResponseStream()))
                //{
                //    int readLength = 2147483647; // br.BaseStream.Length > 2147483647 ? 2147483647 : Convert.ToInt32(br.BaseStream.Length);
                //    b = br.ReadBytes(readLength);
                //    br.Close();
                //}
                context.Response.BinaryWrite(truncated);
            }
        }
        catch (Exception ex)
        {
            //throw ex;
        }
        finally
        {
            context.Response.End();
        }
    }

    // Necessary for IHttpHandler implementation
    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

    public void CopyStream(System.IO.Stream input, System.IO.Stream output)
    {
        //copy the stream in chunks
        byte[] b = new byte[32768];
        int r;
        while ((r = input.Read(b, 0, b.Length)) > 0)
            output.Write(b, 0, r);
    }
}

