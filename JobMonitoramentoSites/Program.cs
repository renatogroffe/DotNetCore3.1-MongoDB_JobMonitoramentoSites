using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Serilog;
using JobMonitoramentoSites.Documents;

namespace JobMonitoramentoSites
{
    class Program
    {
        private const string IDENTIFICACAO_JOB = "JobMonitoramentoSites";

        static void Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile($"appsettings.json")
                 .AddEnvironmentVariables();
                var config = builder.Build();

                var mongoClient = new MongoClient(
                    config.GetConnectionString("BaseMonitoramentoSites"));
                var db = mongoClient.GetDatabase("DBMonitoramento");
                var disponibilidadeCollection =
                    db.GetCollection<ResultadoMonitoramento>("Disponibilidade");

                var sites = config["Sites"]
                    .Split("|", StringSplitOptions.RemoveEmptyEntries);
                foreach (string site in sites)
                {
                    var dadosLog = new ResultadoMonitoramento()
                    {
                        Site = site,
                        LocalExecucao = Environment.MachineName,
                        Horario =
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = new Uri(site);
                        client.DefaultRequestHeaders.Accept.Clear();

                        try
                        {
                            // Envio da requisicao a fim de determinar se
                            // o site esta no ar
                            HttpResponseMessage response =
                                client.GetAsync("").Result;

                            dadosLog.Status = (int)response.StatusCode + " " +
                                response.StatusCode;
                            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                dadosLog.DescricaoErro = response.ReasonPhrase;
                        }
                        catch (Exception ex)
                        {
                            dadosLog.Status = "Exception";
                            dadosLog.DescricaoErro = ex.Message;
                        }
                    }

                    string jsonResultado =
                        JsonSerializer.Serialize(dadosLog);

                    if (dadosLog.DescricaoErro == null)
                        logger.Information(jsonResultado);
                    else
                        logger.Error(jsonResultado);

                    disponibilidadeCollection.InsertOne(dadosLog);
                }

                logger.Information("Execução do Job concluída com sucesso!");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                logger.Error(ex.GetType().FullName + " - " + ex.Message);
                Environment.Exit(1);
            }
        }
    }
}