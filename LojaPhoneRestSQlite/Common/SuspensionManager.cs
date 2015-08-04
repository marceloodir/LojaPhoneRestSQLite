using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace LojaPhoneRestSQlite.Common
{
    /// <summary>
    /// O SuspensionManager captura o estado da sessão global para simplificar o gerenciamento de tempo de vida do processo
    /// para um aplicativo.  Observe que o estado da sessão será limpo automaticamente sob várias
    /// condições e deve ser usado somente para armazenar informações que seriam convenientes para
    /// é transportada entre sessões, mas deve ser descartada quando um aplicativo falha ou é
    /// atualizado.
    /// </summary>
    internal sealed class SuspensionManager
    {
        private static Dictionary<string, object> _sessionState = new Dictionary<string, object>();
        private static List<Type> _knownTypes = new List<Type>();
        private const string sessionStateFilename = "_sessionState.xml";

        /// <summary>
        /// Fornece acesso a um estado de sessão global para a sessão atual.  Este estado é
        /// serializado por <see cref="SaveAsync"/> e restaurado por
        /// <see cref="RestoreAsync"/>, de modo que os valores podem ser serializáveis por
        /// <see cref="DataContractSerializer"/> e deve ser o tão compacto quanto possível.  Cadeias de caracteres
        /// e outros tipos de dados autocontidos são altamente recomendados.
        /// </summary>
        public static Dictionary<string, object> SessionState
        {
            get { return _sessionState; }
        }

        /// <summary>
        /// Lista de tipos personalizados fornecidos ao <see cref="DataContractSerializer"/> quando
        /// ao ler e escrever estado da sessão.  Inicialmente vazios, tipos adicionais podem ser
        /// adicionado para personalizar o processo de serialização.
        /// </summary>
        public static List<Type> KnownTypes
        {
            get { return _knownTypes; }
        }

        /// <summary>
        /// Salvar o <see cref="SessionState"/> atual.  Quaisquer instâncias de <see cref="Frame"/>
        /// registrado com <see cref="RegisterFrame"/> também irá preservar sua atual
        /// pilha de navegação que, por sua vez, dá à sua <see cref="Page"/> ativa uma oportunidade
        /// para salvar seu estado.
        /// </summary>
        /// <returns>Uma tarefa assíncrona que reflete quando o estado da sessão foi salvo.</returns>
        public static async Task SaveAsync()
        {
            try
            {
                // Salve o estado de navegação para todos os quadros registrados
                foreach (var weakFrameReference in _registeredFrames)
                {
                    Frame frame;
                    if (weakFrameReference.TryGetTarget(out frame))
                    {
                        SaveFrameNavigationState(frame);
                    }
                }

                // Serialize o estado da sessão de forma síncrona para evitar acesso assíncrono a
                // estado
                MemoryStream sessionData = new MemoryStream();
                DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, object>), _knownTypes);
                serializer.WriteObject(sessionData, _sessionState);

                // Obter um fluxo de saída para o arquivo SessionState e gravar o estado de forma assíncrona
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(sessionStateFilename, CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await file.OpenStreamForWriteAsync())
                {
                    sessionData.Seek(0, SeekOrigin.Begin);
                    await sessionData.CopyToAsync(fileStream);
                }
            }
            catch (Exception e)
            {
                throw new SuspensionManagerException(e);
            }
        }

        /// <summary>
        /// Restaura o <see cref="SessionState"/> salvo anteriormente.  Quaisquer instâncias de <see cref="Frame"/>
        /// registrado com <see cref="RegisterFrame"/> também irá restaurar sua navegação anterior
        /// estado, que, por sua vez, dá a sua <see cref="Page"/> ativa uma oportunidade de restaurar seu
        /// estado.
        /// </summary>
        /// <param name="sessionBaseKey">Uma chave opcional que identifica o tipo de sessão.
        /// Isso pode ser usado para distinguir entre vários cenários de execução de aplicativos.</param>
        /// <returns>Uma tarefa assíncrona que reflete quando o estado da sessão foi lido.  O
        /// o conteúdo <see cref="SessionState"/> não deverá ser confiado até esta tarefa
        /// concluídas.</returns>
        public static async Task RestoreAsync(String sessionBaseKey = null)
        {
            _sessionState = new Dictionary<String, Object>();

            try
            {
                // Obter o fluxo de entrada para o arquivo SessionState
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(sessionStateFilename);
                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    // Desserializar o Estado da Sessão
                    DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, object>), _knownTypes);
                    _sessionState = (Dictionary<string, object>)serializer.ReadObject(inStream.AsStreamForRead());
                }

                // Restaure quaisquer quadros registrados a seu estado salvo
                foreach (var weakFrameReference in _registeredFrames)
                {
                    Frame frame;
                    if (weakFrameReference.TryGetTarget(out frame) && (string)frame.GetValue(FrameSessionBaseKeyProperty) == sessionBaseKey)
                    {
                        frame.ClearValue(FrameSessionStateProperty);
                        RestoreFrameNavigationState(frame);
                    }
                }
            }
            catch (Exception e)
            {
                throw new SuspensionManagerException(e);
            }
        }

        private static DependencyProperty FrameSessionStateKeyProperty =
            DependencyProperty.RegisterAttached("_FrameSessionStateKey", typeof(String), typeof(SuspensionManager), null);
        private static DependencyProperty FrameSessionBaseKeyProperty =
            DependencyProperty.RegisterAttached("_FrameSessionBaseKeyParams", typeof(String), typeof(SuspensionManager), null);
        private static DependencyProperty FrameSessionStateProperty =
            DependencyProperty.RegisterAttached("_FrameSessionState", typeof(Dictionary<String, Object>), typeof(SuspensionManager), null);
        private static List<WeakReference<Frame>> _registeredFrames = new List<WeakReference<Frame>>();

        /// <summary>
        /// Registra uma instância de <see cref="Frame"/> para permitir que seu histórico de navegação seja salvo em
        /// e restaurado de <see cref="SessionState"/>.  Os quadros devem ser registrados uma vez
        /// imediatamente após a criação se eles forem participar do gerenciamento de estado da sessão.  Mediante
        /// registro se o estado já tiver sido restaurado para a chave especificada
        /// o histórico da navegação será restaurado imediatamente.  Invocações subsequentes de
        /// <see cref="RestoreAsync"/> também irá restaurar o histórico de navegação.
        /// </summary>
        /// <param name="frame">Uma instância cujo histórico de navegação deve ser gerenciado por
        /// <see cref="SuspensionManager"/></param>
        /// <param name="sessionStateKey">Uma chave exclusiva em <see cref="SessionState"/> usada para
        /// armazenar informações relacionadas à navegação.</param>
        /// <param name="sessionBaseKey">Uma chave opcional que identifica o tipo de sessão.
        /// Isso pode ser usado para distinguir entre vários cenários de execução de aplicativos.</param>
        public static void RegisterFrame(Frame frame, String sessionStateKey, String sessionBaseKey = null)
        {
            if (frame.GetValue(FrameSessionStateKeyProperty) != null)
            {
                throw new InvalidOperationException("Frames can only be registered to one session state key");
            }

            if (frame.GetValue(FrameSessionStateProperty) != null)
            {
                throw new InvalidOperationException("Frames must be either be registered before accessing frame session state, or not registered at all");
            }

            if (!string.IsNullOrEmpty(sessionBaseKey))
            {
                frame.SetValue(FrameSessionBaseKeyProperty, sessionBaseKey);
                sessionStateKey = sessionBaseKey + "_" + sessionStateKey;
            }

            // Use uma propriedade de dependência para associar a chave da sessão a um quadro e manter uma lista dos quadros cujos
            // o estado da navegação deve ser gerenciado
            frame.SetValue(FrameSessionStateKeyProperty, sessionStateKey);
            _registeredFrames.Add(new WeakReference<Frame>(frame));

            // Verifique se o estado da navegação pode ser restaurado
            RestoreFrameNavigationState(frame);
        }

        /// <summary>
        /// Desassocia um <see cref="Frame"/> anteriormente registrado por <see cref="RegisterFrame"/>
        /// de <see cref="SessionState"/>.  Qualquer estado de navegação previamente capturado será
        /// removido.
        /// </summary>
        /// <param name="frame">Uma instância cujo histórico de navegação não deve ser superior
        /// gerenciado.</param>
        public static void UnregisterFrame(Frame frame)
        {
            // Remover estado da sessão e remover o quadro da lista de quadros cuja navegação
            // o estado será salvo (juntamente com quaisquer referências que não podem mais ser atingidas)
            SessionState.Remove((String)frame.GetValue(FrameSessionStateKeyProperty));
            _registeredFrames.RemoveAll((weakFrameReference) =>
            {
                Frame testFrame;
                return !weakFrameReference.TryGetTarget(out testFrame) || testFrame == frame;
            });
        }

        /// <summary>
        /// Fornece armazenamento para o estado de sessão associado ao <see cref="Frame"/> especificado.
        /// Quadros que foram previamente registrados com <see cref="RegisterFrame"/> possuem
        /// seus estados de sessão salvos e restaurados automaticamente como parte do global
        /// <see cref="SessionState"/>.  Quadros que não estão registrados possuem estado transiente
        /// que ainda pode ser útil ao restaurar páginas que foram descartadas do
        /// cache de navegação.
        /// </summary>
        /// <remarks>Os aplicativos podem optar por depender do <see cref="NavigationHelper"/> para gerenciar
        /// estado específico de página em vez de trabalhar diretamente com o estado da sessão do quadro.</remarks>
        /// <param name="frame">A instância para a qual o estado da sessão é desejado.</param>
        /// <returns>Uma coleção de estados sujeita ao mesmo mecanismo de serialização que
        /// <see cref="SessionState"/>.</returns>
        public static Dictionary<String, Object> SessionStateForFrame(Frame frame)
        {
            var frameState = (Dictionary<String, Object>)frame.GetValue(FrameSessionStateProperty);

            if (frameState == null)
            {
                var frameSessionKey = (String)frame.GetValue(FrameSessionStateKeyProperty);
                if (frameSessionKey != null)
                {
                    // Quadros registrados refletem o estado da sessão correspondente
                    if (!_sessionState.ContainsKey(frameSessionKey))
                    {
                        _sessionState[frameSessionKey] = new Dictionary<String, Object>();
                    }
                    frameState = (Dictionary<String, Object>)_sessionState[frameSessionKey];
                }
                else
                {
                    // Quadros que não estão registrados possuem estado transiente
                    frameState = new Dictionary<String, Object>();
                }
                frame.SetValue(FrameSessionStateProperty, frameState);
            }
            return frameState;
        }

        private static void RestoreFrameNavigationState(Frame frame)
        {
            var frameState = SessionStateForFrame(frame);
            if (frameState.ContainsKey("Navigation"))
            {
                frame.SetNavigationState((String)frameState["Navigation"]);
            }
        }

        private static void SaveFrameNavigationState(Frame frame)
        {
            var frameState = SessionStateForFrame(frame);
            frameState["Navigation"] = frame.GetNavigationState();
        }
    }
    public class SuspensionManagerException : Exception
    {
        public SuspensionManagerException()
        {
        }

        public SuspensionManagerException(Exception e)
            : base("SuspensionManager failed", e)
        {

        }
    }
}
