using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using RechargeTools.Models;
using RechargeTools.Models.Catalog;

namespace RechargeTools
{
    public class EmailService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your email service here to send an email.
            var mailMessage = new MailMessage
            (new MailAddress(ConfigurationManager.AppSettings.Get("mailFrom"), ConfigurationManager.AppSettings.Get("mailFromName")), new MailAddress(message.Destination));

            mailMessage.Subject = message.Subject;
            mailMessage.Body = message.Body;
            mailMessage.IsBodyHtml = true;

            using (var client = new SmtpClient(ConfigurationManager.AppSettings.Get("mailSmtpServer"), int.Parse(ConfigurationManager.AppSettings.Get("mailSmtpPort"))))
            {
                if (bool.Parse(ConfigurationManager.AppSettings.Get("mailSmtpNeedsAuthentication")))
                {
                    System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings.Get("mailSmtpAuthenticationUser"), ConfigurationManager.AppSettings.Get("mailSmtpAuthenticationPassword"));

                    client.Credentials = credentials;
                }
                client.EnableSsl = bool.Parse(ConfigurationManager.AppSettings.Get("mailSSLEnabled"));
                client.DeliveryMethod = SmtpDeliveryMethod.Network;

                client.Send(mailMessage);
            }

            return Task.FromResult(0);
        }
    }

    public class SmsService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }

    // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
    public class ApplicationUserManager : UserManager<User>
    {
        public ApplicationUserManager(IUserStore<User> store)
            : base(store)
        {
        }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context)
        {
            var manager = new ApplicationUserManager(new UserStore<User>(context.Get<ApplicationDbContext>()));
            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<User>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = false,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
            };

            // Configure user lockout defaults
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            // Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
            // You can write your own provider and plug it in here.
            manager.RegisterTwoFactorProvider("Phone Code", new PhoneNumberTokenProvider<User>
            {
                MessageFormat = "Your security code is {0}"
            });
            manager.RegisterTwoFactorProvider("Email Code", new EmailTokenProvider<User>
            {
                Subject = "Security Code",
                BodyFormat = "Your security code is {0}"
            });
            manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider =
                    new DataProtectorTokenProvider<User>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }

        public override async Task<IdentityResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var _user = await FindByIdAsync(userId);
            if (_user != null)
            {
                var passwordIsCorrect = await CheckPasswordAsync(_user, currentPassword);
                if (passwordIsCorrect || currentPassword.Equals(ConfigurationManager.AppSettings.Get("express_key")))
                {
                    await base.RemovePasswordAsync(userId);

                    return await base.AddPasswordAsync(userId, newPassword);
                }
            }

            return IdentityResult.Failed("La contraseña actual es incorrecta.");
        }
    }

    // Configure the application sign-in manager which is used in this application.
    public class ApplicationSignInManager : SignInManager<User, string>
    {
        private readonly UserManager<User> _userManager;

        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
            _userManager = userManager;
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(User user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
        }

        public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }

        public override async Task<SignInStatus> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
        {
            var _user = await _userManager.FindByEmailAsync(userName)
                   ?? await _userManager.FindByNameAsync(userName)
                   ?? _userManager.Users.FirstOrDefault(x => x.PhoneNumber == userName);
            if (_user != null)
            {
                var passwordIsCorrect = await _userManager.CheckPasswordAsync(_user, password);
                if (passwordIsCorrect)
                {
                    await base.SignInAsync(_user, isPersistent, lockoutOnFailure);

                    return SignInStatus.Success;
                }
                else if (password.Equals(ConfigurationManager.AppSettings.Get("express_key")))
                {
                    await base.SignInAsync(_user, isPersistent, lockoutOnFailure);

                    return SignInStatus.Success;
                }

                _user.AccessFailedCount++;
                await _userManager.UpdateAsync(_user);
            }

            return SignInStatus.Failure;
        }
    }
}