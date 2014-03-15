using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.CatalogUI;
using ESRI.ArcGIS.Catalog;
using ESRI.ArcGIS.Server;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.GISClient;
using ESRI.ArcGIS.SOESupport;
using System.Web.Extensions;
using System.Xml;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Net;
using System.Web.Script.Serialization;


namespace GetManifestTest
{
    public class GetManifest : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        IPropertySet pSet;
        String m_tokenurl;
        String m_username;
        String m_password;
        String m_token;
        String m_logurl;

        public GetManifest() 
        {
            m_tokenurl = "http://localhost:6080/arcgis/admin/generateToken";
            m_username = "arcgis";
            m_password = "arcgis";
            m_logurl = "http://localhost:6080/arcgis/admin/logs/query";
            m_token = "";
        }

        protected override void OnClick()
        {
            IGxApplication gxApplication = (IGxApplication)ArcCatalog.Application;

            IGxAGSObject3 agsObject3 = (IGxAGSObject3)gxApplication.SelectedObject;

            IServerObjectConfiguration config = agsObject3.ServerObjectConfiguration;

            IPropertySet propSet = config.Properties;

            GetProperties(propSet); //Look at each property and value in the Property Set object (logged to output)

            string path = (string)config.Properties.GetProperty("FilePath");

            string mxdPath = "";

            string keyString = ".MapServer";

            if (path.Contains(keyString))
            {
                int indexOf = path.IndexOf(keyString);

                string editPath = path.Remove(indexOf + keyString.Length) + "\\extracted\\manifest.xml";

                XmlDocument xmlDoc = new XmlDocument();  // Read the manifest.xml file to get the MXD file name

                xmlDoc.Load(editPath);

                XmlNodeList nodes = xmlDoc.SelectNodes("//OnPremisePath");

                foreach (XmlNode item in nodes)
                {

                    if (item.ParentNode.Name == "SVCResource" && item.InnerText.Contains(".mxd"))

                        mxdPath = item.InnerText;

                }

            }

            MessageBox.Show(mxdPath);


            //String token = GetToken(m_token);
            //string logresponse = GetLog(m_logurl, token);
            //MessageBox.Show(logresponse.ToString());   
        }

        private string GetToken(string tokenurl)
        {
            NameValueCollection parameters = new NameValueCollection();

            parameters["username"] = m_username;

            parameters["password"] = m_password;

            parameters["client"] = "requestip";

            parameters["referer"] = "";

            parameters["ip"] = "";

            parameters["expiration"] = "10";

            parameters["f"] = "pjson";

            String postData = CreateParameters(parameters);

            string response = PostData(tokenurl, "POST", postData);

            ESRI.ArcGIS.SOESupport.JsonObject json = new JsonObject(response);

            String token = null;

            json.TryGetString("token", out token);

            return token;

        }

        private string GetLog(string logurl, string token)
        {
            Filter filterString = new Filter { server = "*", services = "*", machines = "*"};

            JsonObject json = new JsonObject();

            JavaScriptSerializer oSerializer = new JavaScriptSerializer();

            string filterJSON = oSerializer.Serialize(filterString);

            System.Diagnostics.Debug.WriteLine(filterJSON.ToString());

            NameValueCollection parameters = new NameValueCollection();

            parameters["token"] = token;

            parameters["startTime"] = "1350428400000"; //milis

            parameters["endTime"] = "1350255600000"; //milis

            parameters["level"] = "warning";

            parameters["pageSize"] = "100";

            parameters["filter"] = filterJSON.ToString();

            parameters["f"] = "pjson";

            String postData = CreateParameters(parameters);

            String response = PostData(logurl, "POST", postData);

            return response;

        }

        private string CreateParameters(NameValueCollection keyValues)
        {
            String data = null;

            foreach (string key in keyValues.Keys)
            {
                data += key + "=" + keyValues[key] + "&";
            }

            data = data.Remove(data.Length - 1);

            return data;
        }

        protected void GetProperties(IPropertySet propset)
        {
            object[] nameArray = new object[1];

            object[] valueArray = new object[1];

            propset.GetAllProperties(out nameArray[0], out valueArray[0]);

            object[] names = (object[])nameArray[0];

            object[] values = (object[])valueArray[0];

            System.Text.StringBuilder sb = new StringBuilder();

            for (int i = 0; i < names.Length; i++)
            {
                sb.AppendLine(String.Format("{0}: {1}", names[i], values[i].ToString()));
            }

            System.Diagnostics.Debug.WriteLine(sb.ToString());
        }

        private static string PostData(string requestUri, string method, string postData)
        {
            var result = "";

            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            if (!string.IsNullOrEmpty(requestUri))
            {
                var request = WebRequest.Create(requestUri) as HttpWebRequest;

                if (request != null)
                {
                    request.KeepAlive = true;

                    request.Expect = null;

                    request.ContentType = "application/x-www-form-urlencoded";

                    if (!string.IsNullOrEmpty(method))
                    {
                        request.Method = method;
                    }
                    if (request.Method == "POST")
                    {
                        if (postData != null)
                        {
                            using (var dataStream = request.GetRequestStream())
                            {
                                dataStream.Write(byteArray, 0, byteArray.Length);
                            }
                        }
                    }

                    using (var httpWebResponse = request.GetResponse() as HttpWebResponse)
                    {
                        if (httpWebResponse != null)
                        {
                            using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
                            {
                                result = streamReader.ReadToEnd();
                            }
                            return result;
                        }
                    }
                }
            }
            return "Error";
        }

        protected override void OnUpdate()
        {
            Enabled = ArcCatalog.Application != null;
        }

    }
}