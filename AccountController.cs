using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using PollingBooth.Models;
using PollingBooth.Code;
using MvcSiteMapProvider;
using PollingBoothDAL.Repositories;
using PollingBoothDAL;

namespace PollingBooth.Controllers
{
    public class AccountController : SessionController
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //
        // GET: /Account/LogOn


        public ActionResult LogOn()
        {
           
            return View();
        }

        //
        // POST: /Account/LogOn


        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            

            if (ModelState.IsValid)
            {
                if (Membership.ValidateUser(model.UserName, model.Password))
                {
                    var dbContext = new UsersContext();
                    
                    MembershipUser mUser = Membership.GetUser(model.UserName);
                    string userId = mUser.ProviderUserKey.ToString();


                    aspNetUser user = dbContext.User.Where(t => t.UserId == new Guid(userId)).SingleOrDefault();

                    Session.SetDataInSession("userParentId", user.createdBy==null?"":user.createdBy);

                    var role = Roles.GetRolesForUser(model.UserName);
                    int locationDeatilId =Convert.ToInt32(user.locationDetailId);                    

                    var locationInfo = new customLocationType();

                    // reteive location id and loction Type Id for current logged in user
                    if (locationDeatilId > 0)
                    {
                        locationInfo = utils.fillLocationDetails(locationDeatilId);
                        if (locationInfo == null)
                        {
                            ModelState.AddModelError("", "You have not assigned any location Yet");
                            return View(model);
                        }
                            Session.SetDataInSession("locationDetailId", locationDeatilId);
                            Session.SetDataInSession("locationId", locationInfo.LocationId);
                            Session.SetDataInSession("locationTypeId", locationInfo.LocationTypeId);
                            Session.SetDataInSession("electionListIds", ElectionRepository.fetchElectionforUser(locationInfo.LocationTypeId, Convert.ToInt32(locationInfo.LocationId)));
                            
                    }
                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
                    Session.SetDataInSession("User-IsValid", "1");
                    Session.SetDataInSession("User-UserName", model.UserName);
                    Session.SetDataInSession("userId", userId);
                    Session.SetDataInSession("RoleType", role[0]);
                 
                    //(MvcSiteMapProvider.SiteMap).Refresh();
                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        

                        if (role[0].ToLower() == "subscriber")
                        {
                           if (TaskRepository.IsSubscriptionOver(userId))
                            {
                                
                                Session.SetDataInSession("IsSubscriptionOver", "True");
                            }
                            else
                            {
                                if (TaskRepository.IsUserCountGreaterThanZero(userId))
                                {
                                    Session.SetDataInSession("IsUserCountGreaterThanZero", "True");
                                }
                                else
                                {
                                    Session.SetDataInSession("IsUserCountGreaterThanZero", "False");
                                }
                                Session.SetDataInSession("IsSubscriptionOver", "False");
                            }

                        }

                        if (role[0].ToLower() == "user")
                        {
                            if (TaskRepository.IsSubscriptionOver(user.createdBy))
                            {
                                ModelState.AddModelError("", "Your subscription has been expired, please contact your Administartor.");
                                return View(model);
                            }
                        }
                        //CryptographyRepository cryptoGraphy = new CryptographyRepository();
                        //object[] arrKeys = cryptoGraphy.AssignNewKey();
                        //if (!string.IsNullOrEmpty(arrKeys[0].ToString()))
                        //    Session.SetDataInSession("decryptionKey", arrKeys[0].ToString());
                        //if (!string.IsNullOrEmpty(arrKeys[1].ToString()))
                        //    Session.SetDataInSession("encryptionKey", arrKeys[1].ToString());
                       
                        MvcSiteMapProvider.SiteMaps.ReleaseSiteMap();
                        if (role[0].ToLower() == "administrator")
                        {
                            return RedirectToAction("ElectionType", "ER", new { area = "ElectionResult" });
                        }
                        else
                        {
                            return RedirectToAction("Index", "Search");
                        }
                    }
                }
                else
                {
                    MembershipUser user = Membership.GetUser(model.UserName);
                    if (user != null && user.IsLockedOut)
                    {
                        ModelState.AddModelError("", "This account is locked, please contact administrator.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "The user name or password provided is incorrect.");
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/LogOff

        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();
            Session.Abandon();
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.Now);
            MvcSiteMapProvider.SiteMaps.ReleaseSiteMap();
            //HttpResponse.RemoveOutputCacheItem("/");
            //return Redirect("")
            
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/Register


        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register

        [HttpPost]
        public ActionResult Register(RegisterModel model, string roleName)
        {
            if (ModelState.IsValid)
            {
                // Attempt to register the user
                MembershipCreateStatus createStatus;
                Membership.CreateUser(model.UserName, model.Password, model.Email, null, null, true, null, out createStatus);

                if (createStatus == MembershipCreateStatus.Success)
                {
                    //MembershipUser CurrentUser = Membership.GetUser(User.Identity.Name);
                    //object o=CurrentUser.ProviderUserKey;
                    TempData["myvalue"] = 1;

                    if (!Roles.RoleExists(UserRolesEnum.User.ToString()))
                    {
                        Roles.CreateRole(UserRolesEnum.User.ToString());
                    }


                    FormsAuthentication.SetAuthCookie(model.UserName, false /* createPersistentCookie */);
                    Roles.AddUserToRole(model.UserName, UserRolesEnum.User.ToString());
                    Session.SetDataInSession("User-IsValid", "1");
                    Session.SetDataInSession("User-UserName", model.UserName);
                    //((MvcSiteMapProvider.DefaultSiteMapProvider)SiteMap.Provider).Refresh();
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", ErrorCodeToString(createStatus));
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePassword

        [Authorize]

        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Account/ChangePassword

        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {

                // ChangePassword will throw an exception rather
                // than return false in certain failure scenarios.
                bool changePasswordSucceeded;
                try
                {
                    MembershipUser currentUser = Membership.GetUser(User.Identity.Name, true /* userIsOnline */);
                    changePasswordSucceeded = currentUser.ChangePassword(model.OldPassword, model.NewPassword);
                }
                catch (Exception)
                {
                    changePasswordSucceeded = false;
                }

                if (changePasswordSucceeded)
                {
                    return RedirectToAction("ChangePasswordSuccess");
                }
                else
                {
                    ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePasswordSuccess
        [Authorize]

        public ActionResult ChangePasswordSuccess()
        {
            return View();
        }


        public ActionResult ForgetPassword()
        {
            return View();
        }

        public string SendForgetPassword(string userName, string Email)
        {
            try
            {
                string password = string.Empty;
                if (Email != null)
                    userName = Membership.GetUserNameByEmail(Email);
                if (userName == null)
                    return "ERROR";
                MembershipUser user = Membership.GetUser(userName);
                if (user == null)
                    return "ERROR";
                password = user.GetPassword();
                if (!SendMailUtility.SendMail(user.Email, "Password", password))
                    return "MAILNOTSEND";

                return user.Email;
            }
            catch (MembershipPasswordException exp)
            {
                return "USERLOCKED";
            }
            catch (Exception exp)
            {
                log.Error(exp.Message, exp);
                return "ERROR";
            }
        }

        #region Status Codes
        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
        #endregion

        #region Register UserNameCheck

        public ActionResult DisallowName(string UserName)
        {
            if (Membership.FindUsersByName(UserName).Count <= 0)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            return Json(string.Format("{0} is alread registerd", UserName),
                    JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}
