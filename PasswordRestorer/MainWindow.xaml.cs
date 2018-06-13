using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Input;

namespace PasswordRestorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Random random = new Random();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void cmdRestore_Click(object sender, RoutedEventArgs e)
        {
            using (WaitCursor cursor = new WaitCursor())
            {
                string currentPassword = txtCurrentPsw.Text;
                int historySize = 0;
                int.TryParse(txtHistorySize.Text, out historySize);
                List<string> passwordCache = new List<string>();
                passwordCache.Add(currentPassword);

                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, IPGlobalProperties.GetIPGlobalProperties().DomainName))
                    using (var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, System.Security.Principal.WindowsIdentity.GetCurrent().Name))
                    {
                        for (int i = 0; i < historySize; i++)
                        {
                            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuwxyz0123456789|!£$%&/()=?^'[]1+*@°#-_";
                            string newPassword;

                            do
                            {
                                newPassword = new string(Enumerable.Repeat(chars, 10).Select(s => s[random.Next(s.Length)]).ToArray());
                            }
                            while (passwordCache.Contains(newPassword));

                            user.ChangePassword(currentPassword, newPassword);
                            currentPassword = newPassword;
                            passwordCache.Add(newPassword);
                        }

                        user.ChangePassword(currentPassword, txtPswToRestore.Text);
                        MessageBox.Show("Password restored succesfully!!!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    string rules = $@"The password, to be accepted, must comply with the following rules:
1. it must have at least eight characters.
2. it must be different from the {txtHistorySize.Text} previously used passwords.
3. it must not contain more than two consecutive username characters.
4. it must contain at least three of the following categories:
    a) a minuscule character (a ... z).
    b) an uppercase character (A ... Z).
    c) a number (1 ... 0).
    d) a non-alphanumeric character (for example:!, $, #,%).";

                    MessageBox.Show($"\n\nException: {ex.Message}" + "\n\n" + rules, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    MessageBox.Show($"Current Password: {currentPassword}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void txtHistorySize_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }
    }
}
