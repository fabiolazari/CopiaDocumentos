using System;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Collections.Generic;
using System.Linq;

namespace CopiaDocumentos
{
    //Serviço do windows para copia de arquivos de uma pasta para o google drive
    public class Executa
    {
        private static string diretorioAtual = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        private static void Upload(DriveService servico, string[] ArquivosOrigem, string DiretorioDestino)
        {
            var diretorio = new Google.Apis.Drive.v3.Data.File();
            diretorio.Name = DiretorioDestino;
            diretorio.MimeType = "application/vnd.google-apps.folder";
            diretorio.Id = ProcurarArquivoId(servico, diretorio.Name).First();
            var arquivo = new Google.Apis.Drive.v3.Data.File();
            int i;
            for (i = 0; i < ArquivosOrigem.Length; i++)
            {
                Console.WriteLine(ArquivosOrigem[i]);

                arquivo.Name = Path.GetFileName(ArquivosOrigem[i]);
                arquivo.MimeType = MimeTypes.GetMimeType(Path.GetExtension(ArquivosOrigem[i]));
                arquivo.Parents = new List<string>(new string[] { diretorio.Id });

                using (var stream = new FileStream(ArquivosOrigem[i], FileMode.Open, FileAccess.Read))
                {
                    var ids = ProcurarArquivoId(servico, arquivo.Name);
                    Google.Apis.Upload.ResumableUpload<Google.Apis.Drive.v3.Data.File, Google.Apis.Drive.v3.Data.File> request;
                    if (ids == null || !ids.Any())
                    {
                        var theRequest = servico.Files.Create(arquivo, stream, arquivo.MimeType);
                        theRequest.Fields = "id, parents";
                        request = theRequest;
                    }
                    else
                    {
                        var theRequest = servico.Files.Update(arquivo, ids.First(), stream, arquivo.MimeType);
                        theRequest.Fields = "id, parents";
                        request = theRequest;
                    }
                    request.Upload();
                }
            }
        }

        private static void ListarArquivos(DriveService servico)
        {
            var request = servico.Files.List();
            request.Fields = "files(id, name)";
            var resultado = request.Execute();
            var arquivos = resultado.Files;

            if (arquivos != null && arquivos.Any())
            {
                foreach (var arquivo in arquivos)
                {
                    Console.WriteLine(arquivo.Name);
                }
            }
        }

        private static void Deletar(DriveService servico, string DiretorioDestino)
        {
            var diretorio = new Google.Apis.Drive.v3.Data.File();
            diretorio.Name = DiretorioDestino;
            diretorio.MimeType = "application/vnd.google-apps.folder";
            diretorio.Id = ProcurarArquivoId(servico, diretorio.Name).First();
            var ids = ProcurarAntigoId(servico, diretorio.Id);
            if (ids != null && ids.Any())
            {
                var request = servico.Files.Delete(ids);
                request.Execute();
            }
        }

        private static string ProcurarAntigoId(DriveService servico, string idPasta)
        {
            string retorno = "";
            var Dretorno = new List<DateTime>();
            var Sretorno = new List<string>();
            var DData = new DateTime();
            var request = servico.Files.List();
            request.Q = "'" + idPasta + "' in parents";
            request.Fields = "files(id, name)";
            var resultado = request.Execute();
            var arquivos = resultado.Files;

            if (arquivos != null && arquivos.Any())
            {
                foreach (var arquivo in arquivos)
                {
                    DData = Convert.ToDateTime((arquivo.Name).Substring(11, 10));
                    Dretorno.Add(DData);
                    Sretorno.Add(arquivo.Id);
                }
            }
          
            int i;
            for (i=0;i < Dretorno.Count;i++)
            {
                if (Dretorno[i] == Dretorno.Min())
                {
                    retorno = Sretorno[i];
                }

            }
            return retorno;
        }

        private static string[] ProcurarArquivoId(DriveService servico, string nome, bool procurarNaLixeira = false)
        {
            var retorno = new List<string>();
            var request = servico.Files.List();
            request.Q = string.Format("name = '{0}'", nome);
            if (!procurarNaLixeira)
            {
                request.Q += " and trashed = false";
            }
            request.Fields = "files(id)";
            var resultado = request.Execute();
            var arquivos = resultado.Files;

            if (arquivos != null && arquivos.Any())
            {
                foreach (var arquivo in arquivos)
                {
                    retorno.Add(arquivo.Id);
                }
            }
            return retorno.ToArray();
        }

        private static string[] BuscaArquivos(string caminho)
        {
            List<string> lista = new List<string>();
            DirectoryInfo Dir = new DirectoryInfo(caminho);
            FileInfo[] Files = Dir.GetFiles("*", SearchOption.AllDirectories);
            foreach (FileInfo File in Files)
            {
                string FileName = File.FullName.Replace(Dir.FullName, "");
                lista.Add(Dir + FileName);
            }
            return lista.ToArray();
        }

        private static void CriarDiretorio(DriveService servico, string nomeDiretorio)
        {
            var diretorio = new Google.Apis.Drive.v3.Data.File();
            diretorio.Name = nomeDiretorio;
            diretorio.MimeType = "application/vnd.google-apps.folder";
            var request = servico.Files.Create(diretorio);
            request.Execute();
        }
 
        public static void Envia()
        {
            var credencial = Autenticar();
            using (var servico = AbrirServico(credencial))
            {
                var diretorioConfig = Path.Combine(diretorioAtual, "config.copiadocumentos.txt");
                string[] lines = File.ReadAllLines(diretorioConfig);
                string diretorioOrigem = lines[0];
                string diretorioDestino = lines[1];

                /*Console.WriteLine("Origem: " + diretorioOrigem);
                Console.WriteLine("Destino: " + diretorioDestino);
                Console.ReadKey();*/

                //ListarArquivos(servico);

               // Deletar(servico, diretorioDestino);
              //  Console.WriteLine("Arquivo mais antigo apagado com sucesso...");

                Console.WriteLine("Inicio Cópia Arquivos...");
                Upload(servico, BuscaArquivos(diretorioOrigem), diretorioDestino);
                Console.WriteLine("Fim Cópia Arquivos...");
            }
        }
                    
        private static UserCredential Autenticar()
        {
            UserCredential credenciais;
            using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
                {
                    //var diretorioAtual = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    var diretorioCredenciais = Path.Combine(diretorioAtual, "credential");
                    credenciais = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets,
                        new[] { DriveService.Scope.Drive }, "user", CancellationToken.None, 
                           new FileDataStore(diretorioCredenciais, true)).Result;
                }
            return credenciais;
        }

        private static DriveService AbrirServico(UserCredential credenciais)
        {
            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credenciais
            });
        }
    }
}
