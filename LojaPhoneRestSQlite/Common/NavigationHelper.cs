using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace LojaPhoneRestSQlite.Common
{
    /// <summary>
    /// NavigationHelper ajuda na navegação entre páginas.  Fornece comandos usados para 
    /// voltar e avançar, e registra-se para atalhados padrão de mouse e teclado 
    /// atalhos usado para voltar e avançar no Windows e o botão Voltar de hardware no
    /// Windows Phone. Além disso, integra o SuspensionManger para lidar com a vida útil do processo
    /// gerenciamento e gerenciamento de estado ao navegar entre páginas.
    /// </summary>
    ///<exemplo>
    /// Para usar o NavigationHelper, siga estas duas etapas ou
    /// comece com um BasicPage ou em qualquer outro modelo de item Page diferente de BlankPage.
    /// 
    /// 1) Crie uma instância do NavigationHelper em algum lugar, como em 
    ///     construtor para a página, e registre um retorno de chamada para os eventos LoadState e 
    ///     SaveState.
    ///<código>
    ///     public MyPage()
    ///     {
    ///         this.InitializeComponent();
    ///         var navigationHelper = new NavigationHelper(this);
    ///         this.navigationHelper.LoadState += navigationHelper_LoadState;
    ///         this.navigationHelper.SaveState += navigationHelper_SaveState;
    ///     }
    ///     
    ///     private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
    ///     { }
    ///     private async void navigationHelper_SaveState(object sender, LoadStateEventArgs e)
    ///     { }
    /// </code>
    /// 
    /// 2) Registre a página a ser chamada pelo NavigationHelper sempre que a página participar de 
    ///     da navegação sobrepondo os eventos <see cref="Windows.UI.Xaml.Controls.Page.OnNavigatedTo"/> 
    ///     e <see cref="Windows.UI.Xaml.Controls.Page.OnNavigatedFrom"/>.
    ///<código>
    ///     protected override void OnNavigatedTo(NavigationEventArgs e)
    ///     {
    ///         navigationHelper.OnNavigatedTo(e);
    ///     }
    ///     
    ///     protected override void OnNavigatedFrom(NavigationEventArgs e)
    ///     {
    ///         navigationHelper.OnNavigatedFrom(e);
    ///     }
    /// </code>
    /// </example>
    [Windows.Foundation.Metadata.WebHostHidden]
    public class NavigationHelper : DependencyObject
    {
        private Page Page { get; set; }
        private Frame Frame { get { return this.Page.Frame; } }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="NavigationHelper"/>.
        /// </summary>
        /// <param name="page">Uma referência à página atual usada para navegação.  
        /// Essa referência permite a manipulação de quadros e garantir que as solicitações de 
        /// navegação por teclado ocorram somente quando a página está ocupando a janela inteira.</param>
        public NavigationHelper(Page page)
        {
            this.Page = page;

            //Se esta página for parte da árvore visual faça duas alterações:
            //1) Mapear estado de exibição do aplicativo para o estado visual da página
            // 2) Manipular solicitações de navegação do hardware
            this.Page.Loaded += (sender, e) =>
            {
#if WINDOWS_PHONE_APP
                Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
#else
                //Navegação com teclado e mouse se aplica somente ao ocupar a tela inteira
                if (this.Page.ActualHeight == Window.Current.Bounds.Height &&
                    this.Page.ActualWidth == Window.Current.Bounds.Width)
                {
                    //Ouça a janela diretamente de modo que foco não é necessário
                    Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated +=
                        CoreDispatcher_AcceleratorKeyActivated;
                    Window.Current.CoreWindow.PointerPressed +=
                        this.CoreWindow_PointerPressed;
                }
#endif
            };

            //Desfazer as alterações mesmo quando a página não estiver mais visível
            this.Page.Unloaded += (sender, e) =>
            {
#if WINDOWS_PHONE_APP
                Windows.Phone.UI.Input.HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
#else
                Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated -=
                    CoreDispatcher_AcceleratorKeyActivated;
                Window.Current.CoreWindow.PointerPressed -=
                    this.CoreWindow_PointerPressed;
#endif
            };
        }

        #region Suporte a navegação

        RelayCommand _goBackCommand;
        RelayCommand _goForwardCommand;

        /// <summary>
        /// <see cref="RelayCommand"/> usado para associar à propriedade Command do botão Voltar
        /// para navegar para o item mais recente no histórico de navegação de retorno, se um Frame
        /// gerenciar seu próprio histórico de navegação.
        /// 
        /// O <see cref="RelayCommand"/> é configurado para usar o método virtual <see cref="GoBack"/>
        /// como Executar Ação e <see cref="CanGoBack"/> para CanExecute.
        /// </summary>
        public RelayCommand GoBackCommand
        {
            get
            {
                if (_goBackCommand == null)
                {
                    _goBackCommand = new RelayCommand(
                        () => this.GoBack(),
                        () => this.CanGoBack());
                }
                return _goBackCommand;
            }
            set
            {
                _goBackCommand = value;
            }
        }
        /// <summary>
        /// <see cref="RelayCommand"/> usado para navegar para o item mais recente no 
        /// histórico de navegação de avanço, se um Frame gerenciar seu próprio histórico de navegação.
        /// 
        /// O <see cref="RelayCommand"/> é configurado para usar o método virtual <see cref="GoForward"/>
        /// como Executar Ação e <see cref="CanGoForward"/> para CanExecute.
        /// </summary>
        public RelayCommand GoForwardCommand
        {
            get
            {
                if (_goForwardCommand == null)
                {
                    _goForwardCommand = new RelayCommand(
                        () => this.GoForward(),
                        () => this.CanGoForward());
                }
                return _goForwardCommand;
            }
        }

        /// <summary>
        /// Método virtual usado pela propriedade <see cref="GoBackCommand"/
        /// para determinar se o <see cref="Frame"/> pode voltar.
        /// </summary>
        /// <returns>
        /// true se o <see cref="Frame"/> tiver no mínimo uma entrada 
        /// no histórico de navegação de retorno.
        /// </returns>
        public virtual bool CanGoBack()
        {
            return this.Frame != null && this.Frame.CanGoBack;
        }
        /// <summary>
        /// Método virtual usado pela propriedade <see cref="GoForwardCommand"/
        /// para determinar se o <see cref="Frame"/> pode avançar.
        /// </summary>
        /// <returns>
        /// true se o <see cref="Frame"/> tiver no mínimo uma entrada 
        /// no histórico de navegação de avanço.
        /// </returns>
        public virtual bool CanGoForward()
        {
            return this.Frame != null && this.Frame.CanGoForward;
        }

        /// <resumo>
        /// Método virtual usado pela propriedade <see cref="GoBackCommand"/
        /// para invocar o método <see cref="Windows.UI.Xaml.Controls.Frame.GoBack"/>.
        /// </summary>
        public virtual void GoBack()
        {
            if (this.Frame != null && this.Frame.CanGoBack) this.Frame.GoBack();
        }
        /// <resumo>
        /// Método virtual usado pela propriedade <see cref="GoForwardCommand"/
        /// para invocar o método <see cref="Windows.UI.Xaml.Controls.Frame.GoForward"/>.
        /// </summary>
        public virtual void GoForward()
        {
            if (this.Frame != null && this.Frame.CanGoForward) this.Frame.GoForward();
        }

#if WINDOWS_PHONE_APP
        /// <resumo>
        /// Chamado quando o botão Voltar do hardware é pressionado. Somente para Windows Phone.
        /// </summary>
        /// <param name="sender">Instância que disparou o evento.</param>
        /// <param name="e">Dados de evento que descrevem as condições que levaram ao evento.</param>
        private void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            if (this.GoBackCommand.CanExecute(null))
            {
                e.Handled = true;
                this.GoBackCommand.Execute(null);
            }
        }
#else
        /// <resumo>
        ///Chamado a cada tecla pressionada, incluindo chaves do sistema, tias como combinações de teclas Alt, quando
        ///esta página está ativa e ocupa toda a janela.  Usado para detectar navegação do teclado
        ///entre páginas, mesmo quando a página em si não tem foco.
        /// </summary>
        /// <param name="sender">Instância que disparou o evento.</param>
        /// <param name="e">Dados de evento que descrevem as condições que levaram ao evento.</param>
        private void CoreDispatcher_AcceleratorKeyActivated(CoreDispatcher sender,
            AcceleratorKeyEventArgs e)
        {
            var virtualKey = e.VirtualKey;

            //Somente investigar adiante quando a teclas esquerda direitas ou teclas dedicadas Próximo ou Anterior
            //forem pressionadas
            if ((e.EventType == CoreAcceleratorKeyEventType.SystemKeyDown ||
                e.EventType == CoreAcceleratorKeyEventType.KeyDown) &&
                (virtualKey == VirtualKey.Left || virtualKey == VirtualKey.Right ||
                (int)virtualKey == 166 || (int)virtualKey == 167))
            {
                var coreWindow = Window.Current.CoreWindow;
                var downState = CoreVirtualKeyStates.Down;
                bool menuKey = (coreWindow.GetKeyState(VirtualKey.Menu) & downState) == downState;
                bool controlKey = (coreWindow.GetKeyState(VirtualKey.Control) & downState) == downState;
                bool shiftKey = (coreWindow.GetKeyState(VirtualKey.Shift) & downState) == downState;
                bool noModifiers = !menuKey && !controlKey && !shiftKey;
                bool onlyAlt = menuKey && !controlKey && !shiftKey;

                if (((int)virtualKey == 166 && noModifiers) ||
                    (virtualKey == VirtualKey.Left && onlyAlt))
                {
                    //Ao serem pressionadas a tecla Anterior ou ALt+esquerda navegar para trás
                    e.Handled = true;
                    this.GoBackCommand.Execute(null);
                }
                else if (((int)virtualKey == 167 && noModifiers) ||
                    (virtualKey == VirtualKey.Right && onlyAlt))
                {
                    //Ao serem pressionadas a tecla Seguinte ou ALt+direita navegar adiante
                    e.Handled = true;
                    this.GoForwardCommand.Execute(null);
                }
            }
        }

        /// <resumo>
        ///Chamado a cada clique do mouse, toque na touch screen ou interação equivalente quando esta
        ///página está ativa e ocupa toda a janela.  Usado para detectar avançar em estilo de browser
        ///e cliques anteriores do mouse para navegar entre páginas
        /// </summary>
        /// <param name="sender">Instância que disparou o evento.</param>
        /// <param name="e">Dados de evento que descrevem as condições que levaram ao evento.</param>
        private void CoreWindow_PointerPressed(CoreWindow sender,
            PointerEventArgs e)
        {
            var properties = e.CurrentPoint.Properties;

            //Ignorar combinações com os botões esquerdo, direito e central
            if (properties.IsLeftButtonPressed || properties.IsRightButtonPressed ||
                properties.IsMiddleButtonPressed) return;

            //Navegar apropriadamente se atrás ou adiante forem pressionados, mas não ambos
            bool backPressed = properties.IsXButton1Pressed;
            bool forwardPressed = properties.IsXButton2Pressed;
            if (backPressed ^ forwardPressed)
            {
                e.Handled = true;
                if (backPressed) this.GoBackCommand.Execute(null);
                if (forwardPressed) this.GoForwardCommand.Execute(null);
            }
        }
#endif

        #endregion

        #region Gerenciamento de vida útil de processos

        private String _pageKey;

        /// <resumo>
        /// Registre esse evento na página atual para preencher a página
        /// com o conteúdo passado durante a navegação, bem como qualquer estado
        /// salvo fornecido ao recriar uma página de uma sessão anterior.
        /// </summary>
        public event LoadStateEventHandler LoadState;
        /// <resumo>
        /// Registre esse evento na página atual para preservar
        /// o estado associado à página atual caso o
        /// aplicativo seja suspenso ou a página seja descartada do
        /// cache de navegação.
        /// </summary>
        public event SaveStateEventHandler SaveState;

        /// <resumo>
        /// Invocado quando essa página está prestes a ser exibida em um Frame.  
        /// Este método chama <see cref="LoadState"/>, onde toda a lógica de
        /// gerenciamento de navegação e tempo de vida útil de processo específica da página deve ser usada.
        /// </summary>
        /// <param name="e">Dados de evento que descrevem como essa página foi atingida.  O parâmetro
        /// propriedade fornece o grupo a ser exibido.</param>
        public void OnNavigatedTo(NavigationEventArgs e)
        {
            var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            this._pageKey = "Page-" + this.Frame.BackStackDepth;

            if (e.NavigationMode == NavigationMode.New)
            {
                //Limpar estado existente para avançar na navegação ao adicionar uma nova página à
                //pilha de navegação
                var nextPageKey = this._pageKey;
                int nextPageIndex = this.Frame.BackStackDepth;
                while (frameState.Remove(nextPageKey))
                {
                    nextPageIndex++;
                    nextPageKey = "Page-" + nextPageIndex;
                }

                //Passar o parâmetro de navegação para a nova página
                if (this.LoadState != null)
                {
                    this.LoadState(this, new LoadStateEventArgs(e.Parameter, null));
                }
            }
            else
            {
                //Passar o parâmetro de navegação e o estado preservado para a página usando
                //a mesma estratégia para carregar estados suspenso e recriar páginas descartadas
                //do cache
                if (this.LoadState != null)
                {
                    this.LoadState(this, new LoadStateEventArgs(e.Parameter, (Dictionary<String, Object>)frameState[this._pageKey]));
                }
            }
        }

        /// <resumo>
        ///Chamado quando esta página não é exibida em  frame.
        /// Este método chama <see cref="SaveState"/>, onde toda a lógica de
        /// gerenciamento de navegação e tempo de vida útil de processo específica da página deve ser usada.
        /// </summary>
        /// <param name="e">Dados de evento que descrevem como essa página foi atingida.  O parâmetro
        /// propriedade fornece o grupo a ser exibido.</param>
        public void OnNavigatedFrom(NavigationEventArgs e)
        {
            var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            var pageState = new Dictionary<String, Object>();
            if (this.SaveState != null)
            {
                this.SaveState(this, new SaveStateEventArgs(pageState));
            }
            frameState[_pageKey] = pageState;
        }

        #endregion
    }

    /// <resumo>
    /// Representa o método que administrará o evento <see cref="NavigationHelper.LoadState"/
    /// </summary>
    public delegate void LoadStateEventHandler(object sender, LoadStateEventArgs e);
    /// <resumo>
    /// Representa o método que administrará o evento <see cref="NavigationHelper.SaveState"/
    /// </summary>
    public delegate void SaveStateEventHandler(object sender, SaveStateEventArgs e);

    /// <resumo>
    /// Classe usada para manter os dados de eventos necessários quando uma página tenta carregar o estado.
    /// </summary>
    public class LoadStateEventArgs : EventArgs
    {
        /// <resumo>
        /// O valor do parâmetro passado para <see cref="Frame.Navigate(Type, Object)"/> 
        /// quando esta página inicialmente foi solicitada.
        /// </summary>
        public Object NavigationParameter { get; private set; }
        /// <resumo>
        /// um dicionário de estado preservado por esta página durante uma sessão
        /// anterior.  Será nulo na primeira vez que uma página for visitada.
        /// </summary>
        public Dictionary<string, Object> PageState { get; private set; }

        /// <resumo>
        /// Inicializa uma nova instância da classe <see cref="LoadStateEventArgs"/>.
        /// </summary>
        /// <param name="navigationParameter">
        /// O valor do parâmetro passado para <see cref="Frame.Navigate(Type, Object)"/> 
        /// quando esta página inicialmente foi solicitada.
        /// </param>
        /// <param name="pageState">
        /// um dicionário de estado preservado por esta página durante uma sessão
        /// anterior.  Será nulo na primeira vez que uma página for visitada.
        /// </param>
        public LoadStateEventArgs(Object navigationParameter, Dictionary<string, Object> pageState)
            : base()
        {
            this.NavigationParameter = navigationParameter;
            this.PageState = pageState;
        }
    }
    /// <resumo>
    /// Classe usada para manter os dados de eventos necessários quando uma página tenta salvar o estado.
    /// </summary>
    public class SaveStateEventArgs : EventArgs
    {
        /// <resumo>
        /// Um dicionário vazio a ser preenchido com estado serializável.
        /// </summary>
        public Dictionary<string, Object> PageState { get; private set; }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="SaveStateEventArgs"/>.
        /// </summary>
        /// <param name="pageState">Um dicionário vazio a ser preenchido com o estado serializável.</param>
        public SaveStateEventArgs(Dictionary<string, Object> pageState)
            : base()
        {
            this.PageState = pageState;
        }
    }
}
