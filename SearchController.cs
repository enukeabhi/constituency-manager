using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PollingBoothDAL.ViewModel;
using PollingBoothDAL.Repositories;
using PollingBoothDAL;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Security;
using PollingBoothDAL.Code;
using PollingBooth.Models;

namespace PollingBooth.Controllers
{
    public class SearchController : SessionController
    {
        //
        // GET: /Search/
        [Authorize]
        //[OutputCache(Duration=86400,VaryByParam="LocationId")]
        public ActionResult Index(string id)
        {

            int locationTypeId = Convert.ToInt32(HttpContext.Session["locationTypeId"]);
            int locationId = Convert.ToInt32(HttpContext.Session["locationId"]);
            if (locationTypeId == Convert.ToInt32(TypeofSeats.Statewise))
            {
                ViewData["listStates"] = AreaRepository.GetStateDataWithAssociatedEntities(locationId);//state
            }
            else if (locationTypeId == Convert.ToInt32(TypeofSeats.Parliamentrywise))
            {
                ViewData["listStates"] = AreaRepository.GetParliyamentDataWithAssociatedEntities(locationId);//parliament
            }
            else if (locationTypeId == Convert.ToInt32(TypeofSeats.Assemblywise))
            {
                ViewData["listStates"] = AreaRepository.GetAssemblyDataWithAssociatedEntities(locationId);//assembly
            }
            else if (locationTypeId == Convert.ToInt32(TypeofSeats.Countrywise))
            {
                ViewData["listStates"] = AreaRepository.GetAllStateDataWithAssociatedEntities();//country
            }
            ViewData["SearchedBoothsList"] = new List<SearchBoothDetailViewModel>();
            if (id != null && id != "")
            {
                ViewData["SearchParameter"] = id;
            }


            //List<string[]> allElection = ElectionRepository.fetchAllElectionIdName((List<int>)Session["electionListIds"], false);
            //ViewData["electionList"] = allElection;

            return View();
        }
        [Authorize]
        public ViewResult GetAllBoothsByAssembly(string blockId)
        {
            List<PollingBoothDAL.PollingBooth> booths = AreaRepository.GetAllBoothByBlockId(Convert.ToInt32(blockId));
            ViewData["AllBoothsByAssembly"] = booths;
            return View("_GetAllBoothsByAssembly");
        }
        public ActionResult Autocomplete(string term)
        {
            int locationTypeId = Convert.ToInt32(HttpContext.Session["locationTypeId"]);
            int locationId = Convert.ToInt32(HttpContext.Session["locationId"]);
            List<AutoCompleteLocationViewModel> listAutoCompleteLocationViewModel = LocationRepository.GetAutoCompleteOptions(locationTypeId, locationId);
            // List<AutoCompleteLocationViewModel> listAutoCompleteLocationViewModel =null;
            //int getId = Convert.ToInt32(HttpContext.Application["locationTypeId"]);
            //if (getId == 1)
            //    listAutoCompleteLocationViewModel = LocationRepository.GetAllLocations(true, false, false, false);
            //else if (getId == 2)
            //    listAutoCompleteLocationViewModel = LocationRepository.GetAllLocations(false, true, false, false);
            //else if (getId == 3)
            //    listAutoCompleteLocationViewModel = LocationRepository.GetAllLocations(false, false, true, false);
            //else if (getId == 4)
            //    listAutoCompleteLocationViewModel = LocationRepository.GetAllLocations(false, false, false, true);
            //else { }

            //List<AutoCompleteLocationViewModel> listAutoCompleteLocationViewModel = LocationRepository.GetAllLocations(true, true, true, true);
            List<AutoCompleteLocationViewModel> filterdItems = listAutoCompleteLocationViewModel.Where(x => x.LocationName.StartsWith(term, true, null) || x.UniqueNumber.StartsWith(term) || x.PollingBoothNumber.Trim() == term.Trim()).ToList();
            return Json(filterdItems, JsonRequestBehavior.AllowGet);
        }
         [AjaxRequest]
        public ActionResult GetPollingBoothDataAdvanceSearch(int locationId, int locationTypeId)
        {
            List<SearchBoothDetailViewModel> list = AreaRepository.GetListBoothDataByBlocks(locationId, locationTypeId, 1, 3);
            ViewData["SearchedBoothsList"] = list;
            return PartialView("_PollingBoothDetail");
        }
         [AjaxRequest]
        public ActionResult GetElectionResultDataAdvanceSearch(int locationId, int locationTypeId)
        {
            if (HttpContext.Session.Count > 0)
            {
                int LookForLocationTypeID = locationTypeId;

                string[] Eids = ElectionRepository.GetElectionList((List<int>)Session["electionListIds"], false).Select(x => x.Id.ToString()).ToArray();
                List<ElectionResultFromSQL> List2 = new List<ElectionResultFromSQL>();
                foreach (string elect in Eids)
                {
                    int electionTypeId = ElectionRepository.GetElectionType(Convert.ToInt32(elect));
                    if (locationTypeId <= 2 && electionTypeId == 1)
                    {
                        continue;
                    }
                    string createdBy = Session["userParentId"].ToString() == "" ? Session["userId"].ToString() : Session["userParentId"].ToString();

                    List<ElectionResultFromSQL> erdList = ElectionRepository.GetTotalElectionResult(locationId, locationTypeId, LookForLocationTypeID, Convert.ToInt32(elect), createdBy);
                    List<ElectionResultFromSQL> list = erdList.Where(a => a.votes == erdList.Max(b => b.votes)).ToList();
                    List2.AddRange(list);
                }
                //List<ElectionResultsData> erdList = ElectionRepository.GetTotalElectionResult(locationId, locationTypeId, LookForLocationTypeID,Convert.ToInt32(elect));
                //List<ElectionResultsData> list = erdList.Where(a => a.Votes == erdList.Max(b => b.Votes)).ToList();
                //List2.AddRange(list);
                ViewBag.locType = locationTypeId;
                ViewBag.locId = locationId;
                ViewData["ElectionResultList"] = List2;
            }
			return PartialView("_LastElectionResult");
		}
         [AjaxRequest]
		public ActionResult GetInluentialPersonData(int locationId, int locationTypeId)
		{
            string createdBy = Session["userParentId"].ToString() == "" ? Session["userId"].ToString() : Session["userParentId"].ToString();
            List<InfluencedPerson> list = PersonRepository.GetListOfInfluentialPerson(locationTypeId, locationId, createdBy).Take(3).ToList();
			ViewData["InfluencedPersonData"] = list;
			return PartialView("_InluentialPersonData");
		}
        // [AjaxRequest]
        //public ActionResult GetCasteDetailsDataAdvanceSearch(int locationId, int locationTypeId, string locationName)
        //{
        //    if (HttpContext.Session.Count > 0)
        //    {
        //        string createdBy = Session["userParentId"].ToString() == "" ? Session["userId"].ToString() : Session["userParentId"].ToString();
        //        List<InfluencedPerson> list = PersonRepository.GetListOfInfluentialPerson(locationTypeId, locationId, createdBy).Take(3).ToList();
        //        ViewData["InfluencedPersonData"] = list;
        //    }
           
        //    return PartialView("_InluentialPersonData");
        //}
         [AjaxRequest]
         public ActionResult GetCasteDetailsDataAdvanceSearch(int locationId, int locationTypeId, string locationName)
        {
            if (HttpContext.Session.Count > 0)
            {
                int recordsPerPage = 3;
                int pageIndex = 1;
                string createdBy = Session["userParentId"].ToString() == "" ? Session["userId"].ToString() : Session["userParentId"].ToString();
                List<SearchCasteDetailViewModel> list = Repository.GetListCasteDataByLocationId(locationId, locationTypeId, 1, 3, (List<int>)Session["electionListIds"], createdBy);
                if (recordsPerPage == 0)
                {
                    ViewData["CasteDetailList"] = list;
                }
                else
                {
                    ViewData["CasteDetailList"] = list.ToList().Skip((pageIndex - 1) * recordsPerPage)
                                              .Take(recordsPerPage)
                                              .ToList();
                }
                ViewData["locationName"] = locationName;
            }
            return PartialView("_CasteDetail");
        }
         [AjaxRequest]
        public ActionResult GetPartyWorkerDataAdvanceSearch(int locationId, int locationTypeId)
        {
            if (HttpContext.Session.Count > 0)
            {
               // List<SearchPersonViewModel> list = PersonRepository.GetListPersonDataByLocationId(locationId, locationTypeId, "", "", "", 1, 3);
                List<SearchPersonViewModel> allUser = new List<SearchPersonViewModel>();
                string createdBy = Session["userParentId"].ToString() == "" ? Session["userId"].ToString() : Session["userParentId"].ToString();
                allUser = PersonRepository.GetListOfPartyworkers(false, locationTypeId, locationId, createdBy).Take(3).ToList();
                ViewData["PartyWorkerList"] = allUser;
            }
            return PartialView("_PartyWorkers");
        }
         [AjaxRequest]
        public ActionResult GetMeetingData(int locationId, int locationTypeId)
        {
            if (HttpContext.Session.Count > 0)
            {
                MembershipUser user = Membership.GetUser() as MembershipUser;
               string userName = user.UserName;
               List<Meeting> list = MeetingRepository.GetMeetingList(userName, locationId, locationTypeId);
                ViewData["MeetingList"] = list;
            }
            return PartialView("_Meeting");
        }
        [AjaxRequest]
        public ActionResult GetJansamparkDataAdvanceSearch(int locationId, int locationTypeId)
         {
             if (HttpContext.Session.Count > 0)
             {
                 string createdBy = Session["userParentId"].ToString() == "" ? Session["userId"].ToString() : Session["userParentId"].ToString();
                 List<JansamparkViewModel> list = JanSamparkRepository.GetJansamparkByLocationId(locationId, locationTypeId, createdBy);
                 ViewData["JansamparkList"] = list;
             }
            return PartialView("_Jansampark");
        }
         [AjaxRequest]
        public ActionResult GetDevelopmentWorkAdvanceSearch(int locationId, int locationTypeId)
         {
             if (HttpContext.Session.Count > 0)
             {
                 string createdBy = Session["userParentId"].ToString() == "" ? Session["userId"].ToString() : Session["userParentId"].ToString();
                 List<DevelopmentWorkViewModel> list = DevelopmentWorkRepository.GetDevelopmentWorkByLocationId(locationId, locationTypeId, createdBy);
                 ViewData["DevelopmentWorkList"] = list;
             }
            return PartialView("_DevelopmentWork");
        }
         [AjaxRequest]
        public ActionResult GetIssueAdvanceSearch(int locationId, int locationTypeId)
        {
           
            string createdBy = Session["userParentId"].ToString() == "" ? Session["userId"].ToString() : Session["userParentId"].ToString();
            List<IssueManagementViewModel> list = IssueRepository.GetIssueByLocationId(locationId, locationTypeId, createdBy);
            ViewData["IssueList"] = list;
            return PartialView("_IssueManager");
        }
         [AjaxRequest]
        public ActionResult GetBreadCrumb(int locationId, int locationTypeId)
        {
            if (HttpContext.Session.Count > 0)
            {
                Dictionary<string, string> LocationName = LocationRepository.GetCompleteLocationNameForBreadCrumb(locationId, locationTypeId, null);
                ViewData["LocationName"] = LocationName;

            } return PartialView("_BreadCrumb");
        }
    }
}
