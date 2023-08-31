using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using InterfaceLibraryConfigurator;
using InterfaceLibraryLogger;
using NLog;
using Definitions;

namespace JsonConfigurator
{
    public class JsonConfig : IConfigurator
    {
        private Logger _logger;
        private string _path;
        private Dictionary<string, object> _config;
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
                throw ex;
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
                        list = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(value.ToString()); 
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
                throw ex;
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
                    return value.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                throw ex;
            }
        }
     
        public void init(IGenericLogger logger)
        {
            try
            {
                _logger = (Logger)logger.init("JsonConfigurator");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                throw ex;
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

                _logger.Trace("Fin");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                throw ex;
            }
        }
    
        public bool reLoad()
        {
            try
            {
                _logger.Trace("Inicio");

                load(_path);

                _logger.Trace("Fin");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                throw ex;
            }
        }
    }
}
