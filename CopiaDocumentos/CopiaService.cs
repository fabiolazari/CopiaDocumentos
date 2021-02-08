using System;
using System.ServiceProcess;
using System.Threading;

namespace CopiaDocumentos
{
    public partial class CopiaService : ServiceBase
    {
        private static Thread threadMain;
        private static CancellationTokenSource cst;

        public CopiaService()
        {
            InitializeComponent();
            this.ServiceName = "Copia.Documentos.Service";
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            DateTime data = DateTime.Now;
            string DataFormatada = data.ToString("dd/MM/yyyy");
            int ano = data.Year;
            int mes = data.Month;
            int dia = data.Day;
            int hora = data.Hour;
            int min = data.Minute;
            string linha1 = "Serviço Iniciado as " + hora.ToString("00") + ":" + min.ToString("00") + " horas do dia " + DataFormatada + ", para a copia dos documentos...";
            LogServico.geraLogInformacao(linha1);
#if DEBUG
            while (doWork()) ;
#else
            threadMain = new Thread(new ThreadStart(() => { while (doWork()) ; }));
            threadMain.Start();
#endif
        }

        public static bool doWork()
        {
            cst = new CancellationTokenSource();
            CancellationToken cancelToken = cst.Token;
            DateTime data = DateTime.Now;
            string DataFormatada = data.ToString("dd/MM/yyyy");
            int ano = data.Year;
            int mes = data.Month;
            int dia = data.Day;
            int hora = data.Hour;
            int min = data.Minute;
            string linha1 = "Serviço executado as " + hora.ToString("00") + ":" + min.ToString("00") + " horas do dia " + DataFormatada + ".";
            LogServico.geraLogInformacao(linha1);

            // Executa.Envia();

            System.Diagnostics.Process.Start(@"c:\teste\console.exe");

            Thread.Sleep(60000); //86400000; / --> 24 horas
            return true;
        }

        protected override void OnStop()
        {
            DateTime data = DateTime.Now;
            string DataFormatada = data.ToString("dd/MM/yyyy");
            int ano = data.Year;
            int mes = data.Month;
            int dia = data.Day;
            int hora = data.Hour;
            int min = data.Minute;
            string linha1 = "Serviço parado as " + hora.ToString("00") + ":" + min.ToString("00") + " horas do dia " + DataFormatada + ".";
            LogServico.geraLogInformacao(linha1);
#if DEBUG

#else
            threadMain.Abort();
#endif
        }
    }
}
