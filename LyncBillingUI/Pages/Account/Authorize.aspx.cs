﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using CCC.UTILS.Helpers;
using CCC.UTILS.Libs;
using LyncBillingBase.DataModels;
using LyncBillingUI.Helpers;
using LyncBillingUI.Helpers.Account;

namespace LyncBillingUI.Pages.Account
{
    public partial class Authorize : System.Web.UI.Page
    {
        public AdLib athenticator = new AdLib();
        public string AuthenticationMessage { get; set; }
        public string HeaderAuthBoxMessage { get; set; }
        public string ParagraphAuthBoxMessage { get; set; }
        public string sipAccount = string.Empty;

        private string accessParam = string.Empty;
        private string identityParam = string.Empty;
        private string dropParam = string.Empty;
        private bool redirectionFlag = true;
        private static List<string> AccessLevels { get; set; }

        public UserSession CurrentSession { get; set; }


        protected void Page_Load(object sender, EventArgs e)
        {
            HeaderAuthBoxMessage = string.Empty;
            ParagraphAuthBoxMessage = string.Empty;
            AuthenticationMessage = string.Empty;

            //If the user is not loggedin, redirect to Login page.
            if (HttpContext.Current.Session == null || HttpContext.Current.Session.Contents["UserData"] == null)
            {
                Response.Redirect(GetHomepageLink("login"));
            }
            //but if the user is actually logged in we only need to check if he was granted elevated-access(s)
            else
            {
                //Get a local copy of the user's session.
                CurrentSession = (UserSession)HttpContext.Current.Session.Contents["UserData"];

                //Initialize the list of current user-permissions (user-access-levels!)
                InitAccessLevels();

                //Initialize the redirection flag to true. This is responsible for redirecting the user.
                //In the default state, the user must be redirected unless the request was valid and the redirection_flag was set to false.
                redirectionFlag = true;

                /*
                 * The Users must pass the following autentiaction criteria
                 * PrimarySipAccount = EffectiveSipAccount, which means he is not viewing another's user account (X Person) and this X person seems to have some elevated access permissions.
                 * The asked permission actually exists - in the query string and in the system!
                 * The asked permission was actually granted for the current user.
                 **/

                //Mode 1: Non-delegee requests access
                if (!string.IsNullOrEmpty(CurrentSession.User.SipAccount) && CurrentSession.DelegeeUserAccount == null)
                {
                    if (!string.IsNullOrEmpty(Request.QueryString["access"]) && AccessLevels.Contains(Request.QueryString["access"].ToLower()))
                    {
                        //Case 1: The user asks for Admin or Accounting access
                        //Should pass "access" and "access" should be coherent within our own system
                        //Shouldn't pass the other variables, such as: identity (see the case of "identity" below.
                        //The following condition covers the case in which the user is asking an elevated access-permission
                        if (string.IsNullOrEmpty(Request.QueryString["identity"]))
                        {
                            accessParam = Request.QueryString["access"].ToLower();
                            HeaderAuthBoxMessage = "You have requested an elevated access";
                            ParagraphAuthBoxMessage = "Please note that you must authenticate your information before proceeding any further.";

                            //if the user was authenticated already
                            if (CurrentSession.ActiveRoleName != Functions.NormalUserRoleName && (CurrentSession.IsSiteAdmin || CurrentSession.IsSiteAccountant || CurrentSession.IsDeveloper || CurrentSession.IsDepartmentHead))
                            {
                                Response.Redirect(GetHomepageLink(CurrentSession.ActiveRoleName));
                            }

                            //if the user has the elevated-access-permission s/he is asking for, we fill the access text value in a hidden field in this page's form
                            else if (
                                (accessParam == Functions.SiteAdminRoleName && CurrentSession.IsSiteAdmin) ||
                                (accessParam == Functions.SiteAccountantRoleName && CurrentSession.IsSiteAccountant) ||
                                (accessParam == Functions.SystemAdminRoleName && CurrentSession.IsSystemAdmin) ||
                                (accessParam == Functions.DepartmentHeadRoleName && CurrentSession.IsDepartmentHead) ||
                                CurrentSession.IsDeveloper)
                            {
                                //set the value of hidden field in this page to the value of passed access variable.
                                this.ACCESS_LEVEL_FIELD.Value = accessParam;

                                //The user WOULD HAvE BEEN redirected if s/he weren't granted the elevated-access-permission s/he is asking for. But in this case, they passed the redirection.
                                redirectionFlag = false;
                            }
                        }

                        //Case 2: The user asks for Delegee access
                        if (!string.IsNullOrEmpty(Request.QueryString["identity"]))
                        {
                            accessParam = Request.QueryString["access"].ToLower();
                            identityParam = Request.QueryString["identity"];
                            HeaderAuthBoxMessage = "You have requested to manage a delegee account";
                            ParagraphAuthBoxMessage = "Please note that you must authenticate your information before proceeding any further.";

                            bool userDelegeeCaseMatch = (CurrentSession.IsUserDelegate && accessParam == Functions.UserDelegeeRoleName && CurrentSession.UserDelegateRoles.Find(role => role.ManagedUserSipAccount == identityParam) != null);
                            bool departmentDelegeeCaseMatch = (CurrentSession.IsDepartmentDelegate && accessParam == Functions.DepartmentDelegeeRoleName && CurrentSession.DepartmentDelegateRoles.Find(role => role.ManagedUserSipAccount == identityParam) != null);
                            bool siteDelegeeCaseMatch = (CurrentSession.IsSiteDelegate && accessParam == Functions.SiteDelegeeRoleName && CurrentSession.SiteDelegateRoles.Find(role => role.ManagedUserSipAccount == identityParam) != null);

                            //if the user has the elevated-access-permission s/he is asking for, we fill the access text value in a hidden field in this page's form
                            if (userDelegeeCaseMatch || departmentDelegeeCaseMatch || siteDelegeeCaseMatch || CurrentSession.IsDeveloper)
                            {
                                //set the value of hidden field in this page to the value of passed access variable.
                                this.ACCESS_LEVEL_FIELD.Value = accessParam;
                                this.DELEGEE_IDENTITY.Value = identityParam;
                                //SwitchToDelegeeAndRedirect(identityParam);

                                //The user WOULD HAVE BEEN redirected if s/he weren't granted the elevated-access-permission s/he is asking for. But in this case, they passed the redirection.
                                redirectionFlag = false;
                            }
                        }
                    }
                }

                //The following condition covers the case in which the user is asking to drop the already granted elevated-access-permission
                if (!string.IsNullOrEmpty(Request.QueryString["action"]))
                {
                    dropParam = Request.QueryString["action"].ToLower();

                    //Case 1: Drop Admin or Accounting Access
                    if (dropParam == "drop")
                    {
                        DropAccess(dropParam);
                        redirectionFlag = false;
                    }
                    else
                    {
                        redirectionFlag = true;
                    }
                }


                //if the user was not granted any elevated-access permission or he is currently in a manage-delegee mode, redirect him/her to the Users Dashboard page.
                //Or if the redirection_flag was not set to FALSE so far, we redurect the user to the USER DASHBOARD
                if (redirectionFlag == true)
                {
                    Response.Redirect(GetHomepageLink(Functions.NormalUserRoleName));
                }
            }

            sipAccount = ((UserSession)HttpContext.Current.Session.Contents["UserData"]).User.SipAccount;
        }//END OF PAGE_LOAD


        //
        // This function is responsilbe for initializing the value of the AccessLevels List instance variable
        private void InitAccessLevels()
        {
            if(AccessLevels == null || AccessLevels.Count < 7)
            {
                AccessLevels = new List<string>()
                { 
                    //role_id=20; system-admin
                    Functions.SystemAdminRoleName,

                    //role_id=30; site-admin
                    Functions.SiteAdminRoleName,

                    //role_id=40; site-accountant
                    Functions.SiteAccountantRoleName,

                    //role_id=50; department-head
                    Functions.DepartmentHeadRoleName,

                    //delegee_type=1; user-delegates
                    Functions.UserDelegeeRoleName,

                    //delegee_type=2; department-delegates
                    Functions.DepartmentDelegeeRoleName,

                    //delegee_type=3; site-delegates
                    Functions.SiteDelegeeRoleName
                };
            }
        }


        //
        // This function handles the switching to delegees
        // @param delegeeAddress could be a user sipAccount, a department name or a site name
        private void SwitchToDelegeeAndRedirect(string sipAccount, object delegee, int delegeeType)
        {
            //Initialize a temp copy of the Users Session
            CurrentSession = (UserSession)HttpContext.Current.Session.Contents["UserData"];

            if (delegee is LyncBillingBase.DataModels.User && delegeeType == Global.DATABASE.Roles.UserDelegeeRoleID)
            {
                //Switch identity
                CurrentSession.DelegeeUserAccount = new DelegeeUserAccount();
                CurrentSession.DelegeeUserAccount.DelegeeTypeId = Global.DATABASE.Roles.UserDelegeeRoleID;

                //Get the delegate user account
                CurrentSession.DelegeeUserAccount.User = (LyncBillingBase.DataModels.User)delegee;
                CurrentSession.DelegeeUserAccount.User.DisplayName = HelperFunctions.FormatUserDisplayName(
                    CurrentSession.DelegeeUserAccount.User.FullName, 
                    CurrentSession.DelegeeUserAccount.User.SipAccount, 
                    returnAddressPartIfExists: true);

                //Get the Delegee Phonecalls
                CurrentSession.DelegeeUserAccount.DelegeeUserPhonecalls = new List<PhoneCall>();

                //Get the Delegee Addressbook
                CurrentSession.DelegeeUserAccount.DelegeeUserAddressbook = new Dictionary<string, PhoneBookContact>();

                //Set the ActiveRoleName to "userdelegee"
                CurrentSession.ActiveRoleName = Functions.UserDelegeeRoleName;

                //Redirect to Uer Dashboard
                Response.Redirect(GetHomepageLink(Functions.UserDelegeeRoleName));
            }

            else if (delegee is LyncBillingBase.DataModels.SiteDepartment && delegeeType == Global.DATABASE.Roles.DepartmentDelegeeRoleID)
            {
                //Get delegated department
                CurrentSession.DelegeeUserAccount = new DelegeeUserAccount();
                CurrentSession.DelegeeUserAccount.DelegeeDepartmentAccount = (SiteDepartment)delegee;
                CurrentSession.DelegeeUserAccount.DelegeeTypeId = Global.DATABASE.Roles.DepartmentDelegeeRoleID;

                CurrentSession.DelegeeUserAccount.User = Global.DATABASE.Users.GetBySipAccount(sipAccount);
                CurrentSession.DelegeeUserAccount.User.DisplayName = HelperFunctions.FormatUserDisplayName(
                    CurrentSession.DelegeeUserAccount.User.FullName, 
                    CurrentSession.DelegeeUserAccount.User.SipAccount, 
                    returnAddressPartIfExists: true);

                //Switch ActiveRoleName to Departments Delegee
                CurrentSession.ActiveRoleName = Functions.DepartmentDelegeeRoleName;

                Response.Redirect(GetHomepageLink(Functions.DepartmentDelegeeRoleName));
            }

            else if (delegee is LyncBillingBase.DataModels.Site && delegeeType == Global.DATABASE.Roles.SiteDelegeeRoleID)
            {
                //Get delegated site
                CurrentSession.DelegeeUserAccount = new DelegeeUserAccount();
                CurrentSession.DelegeeUserAccount.DelegeeSiteAccount = (LyncBillingBase.DataModels.Site)delegee;
                CurrentSession.DelegeeUserAccount.DelegeeTypeId = Global.DATABASE.Roles.SiteDelegeeRoleID;

                CurrentSession.DelegeeUserAccount.User = Global.DATABASE.Users.GetBySipAccount(sipAccount);
                CurrentSession.DelegeeUserAccount.User.DisplayName = HelperFunctions.FormatUserDisplayName(
                    CurrentSession.DelegeeUserAccount.User.FullName, 
                    CurrentSession.DelegeeUserAccount.User.SipAccount, 
                    returnAddressPartIfExists: true);

                //Switch ActiveRoleName to Sites Delegee
                CurrentSession.ActiveRoleName = Functions.SiteDelegeeRoleName;

                Response.Redirect(GetHomepageLink(Functions.SiteDelegeeRoleName));
            }
        }


        //
        // This function is responsible for dropping the already-granted elevated-access-permission
        private void DropAccess(string accessParameter)
        {
            //Initialize a temp copy of the Users Session
            CurrentSession = (UserSession)HttpContext.Current.Session.Contents["UserData"];

            //Nullify the DelegeeSipAccount object.
            CurrentSession.DelegeeUserAccount = null;

            //Always set the ActiveRoleName to "user"
            CurrentSession.ActiveRoleName = Functions.NormalUserRoleName;

            //Redirect to the Users Dashboard
            Response.Redirect(GetHomepageLink(Functions.NormalUserRoleName));
        }


        //
        // This function returns the homepage link of a specific role, if given, otherwise it returns the login link.
        private string GetHomepageLink(string roleName = "")
        {
            if (roleName == Functions.SystemAdminRoleName) return "/System/Admin/Dashboard";

            else if (roleName == Functions.SiteAdminRoleName) return "/Site/Administration/Dashboard";
            else if (roleName == Functions.SiteAccountantRoleName) return "/Site/Accounting/Dashboard/";
            else if (roleName == Functions.DepartmentHeadRoleName) return "/Department/Head/Dashboard/";

            else if (roleName == Functions.DepartmentDelegeeRoleName) return "/Delegee/Department/PhoneCalls";
            else if (roleName == Functions.SiteDelegeeRoleName) return "/Delegee/Site/PhoneCalls";
            else if (roleName == Functions.UserDelegeeRoleName) return "/User/Dashboard";

            else if (roleName == Functions.NormalUserRoleName) return "/User/Dashboard";

            //default case
            else return "/Login";
        }


        //
        // This function is responsible for authenticating the user's information.
        protected void AuthenticateUser(object sender, EventArgs e)
        {
            bool status = false;
            AdUserInfo userInfo = new AdUserInfo();

            string msg = string.Empty;
            string user_email = string.Empty;

            //Get the requests from the view.
            string requestedAccessLevel = this.ACCESS_LEVEL_FIELD.Value ?? string.Empty;
            string requestedDelegeeIdentity = this.DELEGEE_IDENTITY.Value ?? string.Empty;

            if (HttpContext.Current.Session != null && HttpContext.Current.Session.Contents["UserData"] != null)
            {
                CurrentSession = (UserSession)HttpContext.Current.Session.Contents["UserData"];
                user_email = CurrentSession.User.SipAccount.ToLower();

                if (this.ACCESS_LEVEL_FIELD != null)
                {
                    status = athenticator.AuthenticateUser(user_email, this.password.Text, out msg);
                    AuthenticationMessage = msg;

                    /** 
                     * -------
                     * To spoof identity for intermediate authentication
                     * status = true;
                     /* --------
                     **/

                    if (status == true)
                    {
                        //System Admin
                        if (requestedAccessLevel == Functions.SystemAdminRoleName)
                        {
                            CurrentSession.ActiveRoleName = Functions.SystemAdminRoleName;
                            Response.Redirect(GetHomepageLink(Functions.SystemAdminRoleName));
                        }

                        //Sites Admin
                        else if (requestedAccessLevel == Functions.SiteAdminRoleName)
                        {
                            CurrentSession.ActiveRoleName = Functions.SiteAdminRoleName;
                            Response.Redirect(GetHomepageLink(Functions.SiteAdminRoleName));
                        }

                        //Sites Accountant
                        else if (requestedAccessLevel == Functions.SiteAccountantRoleName)
                        {
                            CurrentSession.ActiveRoleName = Functions.SiteAccountantRoleName;
                            Response.Redirect(GetHomepageLink(Functions.SiteAccountantRoleName));
                        }

                        //Departments Head
                        else if (requestedAccessLevel == Functions.DepartmentHeadRoleName)
                        {
                            CurrentSession.ActiveRoleName = Functions.DepartmentHeadRoleName;
                            Response.Redirect(GetHomepageLink(Functions.DepartmentHeadRoleName));
                        }

                        //Sites Delegee
                        else if (requestedAccessLevel == Functions.SiteDelegeeRoleName)
                        {
                            var role = CurrentSession.SiteDelegateRoles.Find(someRole => someRole.ManagedSite != null && someRole.ManagedUserSipAccount == requestedDelegeeIdentity);

                            if (role != null)
                            {
                                SwitchToDelegeeAndRedirect(role.ManagedUserSipAccount, role.ManagedSite, Global.DATABASE.Roles.SiteDelegeeRoleID);
                            }
                        }

                        //Departments Delegee
                        else if (requestedAccessLevel == Functions.DepartmentDelegeeRoleName)
                        {
                            var role = CurrentSession.DepartmentDelegateRoles.Find(someRole => someRole.ManagedSiteDepartment != null && someRole.ManagedUserSipAccount == requestedDelegeeIdentity);

                            if (role != null)
                            {
                                SwitchToDelegeeAndRedirect(role.ManagedUserSipAccount, role.ManagedSiteDepartment, Global.DATABASE.Roles.DepartmentDelegeeRoleID);
                            }
                        }

                        //Users Delegee
                        else if (requestedAccessLevel == Functions.UserDelegeeRoleName && this.DELEGEE_IDENTITY != null)
                        {
                            var role = CurrentSession.UserDelegateRoles.Find(someRole => someRole.ManagedUser != null && someRole.ManagedUserSipAccount == requestedDelegeeIdentity);

                            if (role != null)
                            {
                                SwitchToDelegeeAndRedirect(role.ManagedUserSipAccount, role.ManagedUser, Global.DATABASE.Roles.UserDelegeeRoleID);
                            }
                        }

                        //the value of the access_level hidden field has changed - fraud value!
                        CurrentSession.ActiveRoleName = Functions.NormalUserRoleName;
                        Response.Redirect(GetHomepageLink(Functions.NormalUserRoleName));
                    }
                }
                else
                {
                    //the value of the access_level hidden field has changed - fraud value!
                    CurrentSession.ActiveRoleName = Functions.NormalUserRoleName;
                    Response.Redirect(GetHomepageLink(Functions.NormalUserRoleName));
                }

                //Setup the authentication message.
                AuthenticationMessage = (!string.IsNullOrEmpty(AuthenticationMessage)) ? ("* " + AuthenticationMessage) : "";
            }
            else
            {
                Response.Redirect(GetHomepageLink("login"));
            }
        }

    }

}