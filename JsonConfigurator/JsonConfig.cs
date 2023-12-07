using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Timers;
using InterfaceLibraryConfigurator;
using InterfaceLibraryLogger;
using NLog;
using Definitions;
using System.Threading;

namespace JsonConfigurator
{
    public class JsonConfig : IConfigurator
    {
        private Logger _logger;
        private string _path;
        private Dictionary<string, object> _config;
        private Dictionary<string, object> _tempConfig;

        private int _valueTimer = 30;
        private string _strHashConfig;
        private string _tempHash;

        private System.Timers.Timer _timer = new System.Timers.Timer();

        public void addParameter(string parameter, string value)
        {
            try
            {
                _logger.Trace("Inicio");
                List <Dictionary<string, object>> listProcessors = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(_config[Config.Processor].ToString());
                Dictionary<string, object> newProcesorConfig = new Dictionary<string, object>();
                if(listProcessors.Count >= 1)
                {
                    newProcesorConfig = listProcessors[0];
                    newProcesorConfig.Add(parameter, value);

                    listProcessors.RemoveAt(0);
                    listProcessors.Add(newProcesorConfig);

                    _config.Remove(Config.Processor);
                    _config.Add(Config.Processor,listProcessors);
                }
                else
                {
                    throw new Exception($"No fue posible agregar el parametro '{parameter}'");
                }
                _logger.Trace("Fin");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                throw new Exception(ex.ToString());
            }
        }

        public Dictionary<string,object> getMap(string masterKey, string id = null)
        {
            try
            {
                _logger.Trace("Inicio");

                object value;
                if (!_config.TryGetValue(masterKey, out value))
                {
                    _logger.Error("No se encontro el parametro '{0}' en el fichero de configuracion", masterKey);
                    throw new Exception($"No se encontro el parametro '{masterKey}' en el fichero de configuracion");
                }
                else
                {
                    bool encontrado = false;
                    List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
                    string tem = value.GetType().ToString();
                    if (value.GetType().ToString()== "System.Text.Json.JsonElement")
                    {
                        list = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(value.ToString()); 
                    }
                    else
                    {
                        list = value as List<Dictionary<string, object>>;
                    }

                    if (id is not null && list is not null && list.Count > 0)
                    {
                        for (int i=0; i<list.Count; i++)
                        {
                            if (list[i]["id"].ToString() == id)
                            {
                                Dictionary<string, object> dirConfig = list[i];
                                encontrado = true;
                                return dirConfig;
                            }
                        }
                        if (encontrado is false)
                        {
                            throw new Exception($"No se encontro el id '{id}' en '{masterKey}'");
                        }
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                throw new Exception(ex.ToString());
            }
        }

        public string getValue(string masterKey, string id = null)
        {
            try
            {
                _logger.Trace("Inicio");

                object value;
                if (!_config.TryGetValue(masterKey, out value))
                {
                    _logger.Error("No se encontro el parametro '{0}' en el fichero de configuracion", masterKey);
                    throw new Exception($"No se encontro el parametro '{masterKey}' en el fichero de configuracion");
                }
                else
                {
                    _logger.Trace("Fin");
                    return value.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                throw new Exception(ex.ToString());
            }
        }
     
        public void init(IGenericLogger logger)
        {
            try
            {
                _logger = (Logger)logger.init("JsonConfigurator");

                _timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                throw new Exception(ex.ToString());
            }
        }

        public void load(string path)
        {
            try
            {
                _logger.Trace("Inicio");
                _path = path;
                string jsonConfig = File.ReadAllText(path);
                _config = JsonSerializer.Deserialize<Dictionary<string,object>>(jsonConfig);

                _strHashConfig = makeHashProcessorSection(_config);

                object value;
                if (_config.TryGetValue("module", out value))
                {
                    Dictionary<string, object> dModule = JsonSerializer.Deserialize<Dictionary<string, object>>(value.ToString()); ;
                    if (dModule.TryGetValue("watchConfig", out value))
                    {
                        _valueTimer = Convert.ToInt32(value.ToString());
                    }
                }
                _logger.Debug($"'watchConfig' establecido en {_valueTimer} segundos");

                _config.Add("watchConfig", _valueTimer);

                _timer.Interval = _valueTimer * 1000;
                _timer.Enabled = true;

                _logger.Trace("Fin");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                throw new Exception(ex.ToString());
            }
        }
    
        public bool hasNewConfig()
        {
            try
            {
                bool newConfig = _strHashConfig != _tempHash;
                if (newConfig)
                {
                    _config = _tempConfig;
                    _strHashConfig = _tempHash;
                }
                return newConfig;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                throw new Exception(ex.ToString());
            }
        }

        private string makeHashProcessorSection(Dictionary<string, object> config)
        {
            try
            {
                object value;
                if (!config.TryGetValue("processors", out value))
                {
                    _logger.Error("No se encontro el parametro 'processors' en el fichero de configuracion");
                    throw new Exception("No se encontro el parametro 'processors' en el fichero de configuracion");
                }
                return HashString(value.ToString());
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                throw new Exception(ex.ToString());
            }
        }

        private string HashString(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return String.Empty;
            }

            // Uses SHA256 to create the hash
            using (var sha = SHA1.Create())
            {
                // Convert the string to a byte array first, to be processed
                byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(text);
                byte[] hashBytes = sha.ComputeHash(textBytes);

                // Convert back to a string, removing the '-' that BitConverter adds
                string hash = BitConverter
                    .ToString(hashBytes)
                    .Replace("-", String.Empty);

                return hash;
            }
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            string jsonConfig = File.ReadAllText(_path);
            _tempConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonConfig);
            _tempHash = makeHashProcessorSection(_tempConfig);
        }
    }
}
