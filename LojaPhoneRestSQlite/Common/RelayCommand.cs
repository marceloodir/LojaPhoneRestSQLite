using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LojaPhoneRestSQlite.Common
{
    /// <summary>
    /// Um comando cujo único propósito é retransmitir sua funcionalidade 
    /// a outros objetos invocando delegados. 
    /// O valor de retorno padrão para o método CanExecute é 'true'.
    /// <see cref="RaiseCanExecuteChanged"/> precisa ser chamado sempre que
    /// <see cref="CanExecute"/> for esperado para retornar um valor diferente.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Lançado quando RaiseCanExecuteChanged é chamado.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Cria um novo comando que pode ser sempre executado.
        /// </summary>
        /// <param name="execute">A lógica de execução.</param>
        public RelayCommand(Action execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Cria um novo comando.
        /// </summary>
        /// <param name="execute">A lógica de execução.</param>
        /// <param name="canExecute">A lógica de status de execução.</param>
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determina se este <see cref="RelayCommand"/> pode ser executado em seu estado atual.
        /// </summary>
        /// <param name="parameter">
        /// Dados usados pelo comando. Se o comando não exigir que os dados sejam passados, esse objeto pode ser configurado como nulo.
        /// </param>
        /// <returns>verdadeiro se esse comando puder ser executado; caso contrário, falso.</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute();
        }

        /// <summary>
        /// Executa o <see cref="RelayCommand"/> no atual destino do comando.
        /// </summary>
        /// <param name="parameter">
        /// Dados usados pelo comando. Se o comando não exigir que os dados sejam passados, esse objeto pode ser configurado como nulo.
        /// </param>
        public void Execute(object parameter)
        {
            _execute();
        }

        /// <summary>
        ///Método utilizado para acionar o evento <see cref="CanExecuteChanged"/>
        /// para indicar que o valor de retorno do <see cref="CanExecute"/>
        /// o método mudou.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}