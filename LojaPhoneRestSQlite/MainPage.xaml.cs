using LojaPhoneRestSQlite.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using SQLite;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


// O modelo de item de Página em Branco é documentado em http://go.microsoft.com/fwlink/?LinkId=391641

namespace LojaPhoneRestSQlite
{
    /// <resumo>
    /// Uma página vazia que pode ser usada sozinho ou navigated para dentro de um Frame.
    /// </resumo>
    public sealed partial class MainPage : Page
    {
        private string ip = "http://192.168.1.107";

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

           //this.CarregandoListagemLocal();
        }

        /// <resumo>
        /// Chamado quando esta página é exibida num Frame.
        /// </resumo>
        /// <param name="e">Dados de evento que descrevem como essa página foi atingida.
        /// Este parâmetro normalmente é usada para configurar a página.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare a página para exibição aqui.

            // TODO: Se seu aplicativo contiver várias páginas certifique-se de que esteja
            // manipulando o botão Voltar do hardware ao fazer o registro para o
            // Evento Windows.Phone.UI.Input.HardwareButtons.BackPressed.
            // Se estiver usando o NavigationHelper fornecido por alguns modelos,
            // este evento é manipulado para você.
        }

        private async void BaixarListagemClick(object sender, RoutedEventArgs e)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(ip);
            var response = await httpClient.GetAsync("/lojarest/api/teste?entrada=loja");
            var str = response.Content.ReadAsStringAsync().Result;
            List<string> cmds = JsonConvert.DeserializeObject<List<string>>(str);
            //string obj = JsonConvert.DeserializeObject<string>(str);
            //string[] cmds = obj.Split('#');
            System.Diagnostics.Debug.WriteLine("############################Dados brutos## " + cmds.Count());

            DataBaseHelperAccess AcessoBanco = new DataBaseHelperAccess();
            foreach (var c in cmds)
            {
               System.Diagnostics.Debug.WriteLine("******************************Dados Trabalhados## " + c);
               AcessoBanco.criando_populando(c);
            }
            MessageDialog msgbox = new MessageDialog("Dados carregados localmente.");
            await msgbox.ShowAsync();
            //List<Models.Fabricante> obj = JsonConvert.DeserializeObject<List<Models.Fabricante>>(str);
            //ListBoxFabricantes.ItemsSource = obj;
            //this.GravandoListagemLocal(obj);
        }

        private void CarregandoListagemLocal()
        {
            DataBaseHelperAccess AcessoBanco = new DataBaseHelperAccess();
            ListBoxFabricantes.ItemsSource = AcessoBanco.LendoFabricantesLocal();
        }

        private void GravandoListagemLocal(List<Fabricante> fabricantes)
        {
            //DataBaseHelperAccess AcessoBanco = new DataBaseHelperAccess();
            //AcessoBanco.InserindoListaFabricantes(fabricantes);
        }

        private void LocalClick(object sender, RoutedEventArgs e)
        {
            DataBaseHelperAccess AcessoBanco = new DataBaseHelperAccess();
            List<Fabricante> fabricantes = AcessoBanco.LendoFabricantesLocal();
            System.Diagnostics.Debug.WriteLine("############################Dados LOCAIS## " + fabricantes[0].descricao);
            ListBoxFabricantes.ItemsSource = fabricantes;


        }

    }
}
