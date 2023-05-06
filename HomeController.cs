using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlacesGuide.Entity.Entitys;
using PlacesGuide.Entity.Entitys.CPanel;
using PlacesGuide.Entity.Entitys.Home;
using PlacesGuide.Helper;
using PlacesGuide.Models;
using PlacesGuide.Repository.Interfaces.Generic;
using PlacesGuide.Web.Models.DTO;
using PlacesGuide.Web.Models.ViewModel;
using PlacesGuide.Web.Models.Enums;
using PlacesGuide.Web.Helper;
using PlacesGuide.Entity.Entitys.CPanel.Prodect;

namespace PlacesGuide.Web.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHostingEnvironment _IHostingEnvironment;
        protected readonly IBaseRepository<ContactUs> _contactUsRepository;
        protected readonly IBaseRepository<SiteImages> _siteImagesRepository;
        protected readonly IBaseRepository<Place> _placeRepository;
        protected readonly IBaseRepository<PlaceImages> _placeImagesRepository;
        protected readonly IBaseRepository<ViewPlaces> _viewPlacesRepository;
        protected readonly IBaseRepository<SuggestedPlace> _suggestedPlaceRepository;
        protected readonly IBaseRepository<Category> _categoryRepository;
        protected readonly IBaseRepository<Offer> _offerRepository;
        protected readonly IBaseRepository<PlaceProdect> _placeProdectRepository;
        protected readonly IBaseRepository<HomeComponents> _homeComponentsRepository;
        protected readonly IBaseRepository<AddedPlace> _addedPlaceRepository;
        protected readonly IBaseRepository<Homeslider> _homeSliderRepository;

        protected readonly IMapper _mapper;

        public HomeController(
            ILogger<HomeController> logger,
            IHostingEnvironment IHostingEnvironment,
            IBaseRepository<ContactUs> contactUsRepository,
            IBaseRepository<SiteInfo> siteInfoRepository,
            IBaseRepository<HomeComponents> homeComponentsRepository,
            IBaseRepository<SiteImages> siteImagesRepository,
            IBaseRepository<Place> placeRepository,
            IBaseRepository<PlaceImages> placeImagesRepository,
            IBaseRepository<ViewPlaces> viewPlacesRepository,
            IBaseRepository<SuggestedPlace> suggestedPlaceRepository,
            IBaseRepository<Category> categoryRepository,
            IBaseRepository<Offer> offerRepository,
            IBaseRepository<PlaceProdect> placeProdectRepository,
            IBaseRepository<AddedPlace> addedPlaceRepository,
            IBaseRepository<Homeslider> homeSliderRepository,
            IMapper mapper) : base(siteInfoRepository, homeComponentsRepository)
        {
            _logger = logger;
            _contactUsRepository = contactUsRepository;
            _siteImagesRepository = siteImagesRepository;
            _placeRepository = placeRepository;
            _placeImagesRepository = placeImagesRepository;
            _viewPlacesRepository = viewPlacesRepository;
            _suggestedPlaceRepository = suggestedPlaceRepository;
            _categoryRepository = categoryRepository;
            _offerRepository = offerRepository;
            _placeProdectRepository = placeProdectRepository;
            _mapper = mapper;
            _IHostingEnvironment = IHostingEnvironment;
            _homeComponentsRepository = homeComponentsRepository;
            _addedPlaceRepository = addedPlaceRepository;
            _homeSliderRepository = homeSliderRepository;
        }

        public async Task<IActionResult> Index()
        {
            var homeCompononts = _homeComponentsRepository.Get().FirstOrDefault();
            

            var HC = new HomeComponents
            {
                MainTitle = " ",
                SubTitle = " ",
                MainTitleColor = "#ffffff",
                SubTitleColor = "#ffffff",
                MainBackgroundImage = " ",
                MainTitlePlaces = " ",
                SubTitlePlaces = " ",
                MainTitleAbout = " ",
                SubTitleOffers = " ",
                MainTitleOffers = " ",
                SubTitleAbout = " ",
                AboutBackgroundImage = " ",
                LinkVideoAbout = " ",
            };
            var vm = new HomeVM();
            vm.HomeComponents = homeCompononts == null ? HC : homeCompononts;
            vm.HomeSliderLst = _homeSliderRepository.Filter(a => a.IsActive == true)
                .Select(x=>new HomeSliderVM
                {Id=x.Id,
                Name=x.Name,
                Path=x.Path,
                Url=x.Url
                }).ToList();
           

            vm.Places = await _placeRepository.Filter((x => x.IsActive && x.ViewType.Equals(ViewType.Home.ToString())), 0, 10)
                .Select(x => new Place
                {
                    Name = x.Name,
                    MainImage = x.MainImage,
                    MainCategory = x.MainCategory,
                    Address = x.Address,
                    Id = x.Id,
                    Descrption = HtmlToString.ConvertHtmlToText(x.Descrption).Length > 70 ? HtmlToString.ConvertHtmlToText(x.Descrption).Substring(0, 70) + "..." : HtmlToString.ConvertHtmlToText(x.Descrption)
                }).ToListAsync();
            return View(vm);
        }
        public IActionResult SuggestedPlace()
        {
            return View();
        }
        public async Task<IActionResult> Offers(int id, OfferDTO offerDTO)
        {
            var numberofpages = Math.Ceiling(_offerRepository.GetCount(x => x.IsActive && (x.Name.Contains(offerDTO.SearchKey)
            || string.IsNullOrEmpty(offerDTO.SearchKey) &&
                (!offerDTO.Place.HasValue || x.PlaceId == offerDTO.Place.Value))) / 15.0);
            if (id < 1 || id > numberofpages) id = 1;
            var skipVal = (id - 1) * 15;
            var query = await _offerRepository.Filter(
                filter: x => x.IsActive && (x.Name.Contains(offerDTO.SearchKey) || string.IsNullOrEmpty(offerDTO.SearchKey) &&
                (!offerDTO.Place.HasValue || x.PlaceId == offerDTO.Place.Value)), skipVal, 15,
                orderBy: x => x.OrderBy(x => x.CreateAt),
                 include: x => x.Include(c => c.Place)).ToListAsync();
            ViewData["Places"] = new SelectList(await _placeRepository.Find(x => x.PlaceBranchId == null), "Id", "Name");
            ViewBag.numberofpages = numberofpages;
            ViewBag.id = id;
            return View(query);
        }

        public async Task<IActionResult> Places(int id)
        {
            var numberofpages = Math.Ceiling(_placeRepository.GetCount(x => x.IsActive && x.PlaceBranchId == null) / 15.0);
            if (id < 1 || id > numberofpages) id = 1;
            var skipVal = (id - 1) * 15;
            var query = await _placeRepository.Filter(
                           filter: x => x.IsActive && x.PlaceBranchId == null , skipVal, 15,
                           orderBy: x => x.OrderBy(x => x.OrderId),
                           include: x => x.Include(c => c.MainCategory)).Select(x => new Place
                           {
                               Name = x.Name,
                               MainImage = x.MainImage,
                               MainCategory = x.MainCategory,
                               Address = x.Address,
                               Id = x.Id,
                               Descrption = HtmlToString.ConvertHtmlToText(x.Descrption).Length > 70 ? HtmlToString.ConvertHtmlToText(x.Descrption).Substring(0, 70) + "..." : HtmlToString.ConvertHtmlToText(x.Descrption)
                           }).ToListAsync();
    

            ViewData["Category"] = new SelectList(await _categoryRepository.Find(x => x.ParentCategoryId == null), "Id", "Name");
            return View(query);
        }
        public async Task<IActionResult> SearchPlaces(int id, PlacesDTO placesDTO)
        {
            var numberofpages = Math.Ceiling(_placeRepository.GetCount(x => x.IsActive && x.PlaceBranchId == null && (x.Name.Contains(placesDTO.SearchKey) || string.IsNullOrEmpty(placesDTO.SearchKey)) && (x.SubCategoryId == placesDTO.SubCategory || !placesDTO.SubCategory.HasValue) && (x.MainCategoryId == placesDTO.Category || !placesDTO.Category.HasValue)) / 15.0);
            if (id < 1 || id > numberofpages) id = 1;
            var skipVal = (id - 1) * 15;
            var query = await _placeRepository.Filter(
                           filter: x => x.IsActive && x.PlaceBranchId == null && (x.Name.Contains(placesDTO.SearchKey) || string.IsNullOrEmpty(placesDTO.SearchKey)) && ((x.MainCategoryId == placesDTO.Category || x.SubCategoryId == placesDTO.Category) || !placesDTO.Category.HasValue) && (x.SubCategoryId == placesDTO.SubCategory || !placesDTO.SubCategory.HasValue), skipVal, 15,
                           orderBy: x => x.OrderBy(x => x.OrderId),
                           include: x => x.Include(c => c.MainCategory)).Select(x => new Place
                           {
                               Name = x.Name,
                               MainImage = x.MainImage,
                               MainCategory = x.MainCategory,
                               Address = x.Address,
                               Id = x.Id,
                               Descrption = HtmlToString.ConvertHtmlToText(x.Descrption).Length > 70 ? HtmlToString.ConvertHtmlToText(x.Descrption).Substring(0, 70) + "..." : HtmlToString.ConvertHtmlToText(x.Descrption)
                           }).ToListAsync();
            if (placesDTO.SortType == Models.Enums.SortType.NewlyAdded)
            {
                query = query.OrderByDescending(x => x.Id).ToList();
            }
            else if (placesDTO.SortType == Models.Enums.SortType.FromTheOldestToTheMostRecent)
            {
                query = query.OrderBy(x => x.Id).ToList();

            }
            else if (placesDTO.SortType == Models.Enums.SortType.TheMostPopular)
            {
                query = await _viewPlacesRepository.Filter(
                           filter: x => x.Place.IsActive && x.Place.PlaceBranchId == null && (x.Place.Name.Contains(placesDTO.SearchKey) || string.IsNullOrEmpty(placesDTO.SearchKey)) && (x.Place.MainCategoryId == placesDTO.Category || !placesDTO.Category.HasValue) && (x.Place.SubCategoryId == placesDTO.SubCategory || !placesDTO.SubCategory.HasValue), skipVal, 15,
                           orderBy: x => x.OrderByDescending(x => x.TotalCount),
                           include: x => x.Include(c => c.Place).ThenInclude(x => x.MainCategory)).Select(x => new Place
                           {
                               Name = x.Place.Name,
                               MainImage = x.Place.MainImage,
                               MainCategory = x.Place.MainCategory,
                               Address = x.Place.Address,
                               Id = x.Place.Id,
                               Descrption = HtmlToString.ConvertHtmlToText(x.Place.Descrption).Length > 70 ? HtmlToString.ConvertHtmlToText(x.Place.Descrption).Substring(0, 70) + "..." : HtmlToString.ConvertHtmlToText(x.Place.Descrption)
                           }).ToListAsync();

            }
            ViewBag.numberofpages = numberofpages;
            ViewBag.id = id;

            ViewData["Category"] = new SelectList(await _categoryRepository.Find(x => x.ParentCategoryId == null), "Id", "Name");
            return View("Places",query);
        }
        public async Task<IActionResult> DetailsPlace(int id)
        {
            var model = await _placeRepository.Get()
                .Include(x => x.Area)
                .Include(x => x.Province)
                .Include(x => x.City)
                .Include(x => x.District).SingleOrDefaultAsync(x => x.Id == id);
            if (model == null)
            {
                return NotFound();
            }
            var list = await _placeRepository.Filter(x => x.MainCategoryId == model.MainCategoryId, 0, 8).ToListAsync();
            var branches = await _placeRepository.Filter(
                           filter: x => x.IsActive && x.PlaceBranchId == model.Id,
                           orderBy: x => x.OrderBy(x => x.CreateAt),
                           include: x => x.Include(c => c.MainCategory).Include(x => x.PlaceProdects).ThenInclude(x => x.Prodect)).ToListAsync();

            var offers = await _offerRepository.Filter(
                          filter: x => x.IsActive && x.PlaceId == model.Id,
                          orderBy: x => x.OrderBy(x => x.CreateAt)).ToListAsync();

            var placeProdect = await _placeProdectRepository.Filter(
                          filter: x => x.Prodect.IsActive && x.PlaceId == model.Id,
                          include: x => x.Include(x => x.Prodect),
                          orderBy: x => x.OrderBy(x => x.Prodect.CreateAt)).ToListAsync();

            var placeImages = await _placeImagesRepository.Filter(
                          filter: x => true && x.PlaceId == id,
                          orderBy: x => x.OrderBy(x => x.CreateAt.Value)).ToListAsync();

            var vm = new PlaceDetailsVM
            {
                Place = model,
                RealtedPlaces = list,
                Branches = branches,
                Offers = offers,
                PlaceProdects = placeProdect,
                PlaceImages = placeImages
            };
            var itemCount = await _viewPlacesRepository.FindSingle(x => x.PlaceId == id);
            if (itemCount != null)
            {
                itemCount.TotalCount = itemCount.TotalCount + 1;
                await _viewPlacesRepository.UpdateAsync(itemCount);
            }
            return View(vm);
        }
        public async Task<IActionResult> CategoryAsync(int? id)
        {
            var data = new List<CategoryHomeVM>();
            var dataCategory = await _categoryRepository.Filter(
                   filter: x => x.IsActive,
                   orderBy: x => x.OrderBy(x => x.CreateAt)).ToListAsync();
            if (!id.HasValue)
            {
                data = dataCategory.Select(x => new CategoryHomeVM
                {
                    Id = x.Id,
                    Name = x.Name,
                    ImageName = x.ImageName,
                    OrderId = _placeRepository.GetCount(m => m.MainCategoryId == x.Id),
                    IsParent = _categoryRepository.Any(c => c.ParentCategoryId == x.Id).Result
                }).OrderBy(x => x.OrderId).ToList();
            }
            else
            {
                data = dataCategory.Select(x => new CategoryHomeVM
                {
                    Id = x.Id,
                    Name = x.Name,
                    ImageName = x.ImageName,
                    OrderId = _placeRepository.GetCount(m => m.SubCategoryId == x.Id),
                    IsParent = _categoryRepository.Any(c => c.ParentCategoryId == x.Id).Result
                }).OrderByDescending(x => x.OrderId).ToList();
            }
            if (data.Any())
            {
                return View(data);
            }
            else
            {
                return RedirectToAction("Places", new { SubCategory = id });
            }
        }
        public async Task<IActionResult> AboutUs()
        {
            var siteImages = await _siteImagesRepository.GetAll();

            return View(siteImages);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public string Contact(HomeVM contact)
        {
            if (ModelState.IsValid)
            {
                var model = _mapper.Map<ContactUs>(contact.ContactUsVM);
                _contactUsRepository.AddAsync(model);
                return "success";
            }
            return "";
        }

        [HttpPost]
        public async Task<string> Suggested(HomeVM suggested, IFormFile ImageName)
        {
            if (ModelState.IsValid)
            {
                var model = _mapper.Map<SuggestedPlace>(suggested.SuggestedVM);
                if (ImageName != null)
                {
                    model.PlaceImage = await ImageHelper.SaveImage(ImageName, _IHostingEnvironment, "Images/PlaceImages");
                }
                else
                {
                    model.PlaceImage = "default-thumbnail.jpg";
                }

                var result = _suggestedPlaceRepository.AddAsync(model);

                return "success";
            }
            return "";
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> AddedPlace()
        {
            ViewData["Category"] = new SelectList(await _categoryRepository.Find(x => x.ParentCategoryId == null), "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<string> AddedPlace(AddedPlaceVM AddedPlace)
        {
            if (ModelState.IsValid)
            {
                var model = _mapper.Map<AddedPlace>(AddedPlace);

                var result = await _addedPlaceRepository.AddAsync(model);
                if (result != null)
                    return "success";
                return "";
            }
            return "";
        }

    }
}
