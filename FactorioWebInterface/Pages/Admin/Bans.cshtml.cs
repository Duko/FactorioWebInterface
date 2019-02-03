﻿using FactorioWebInterface.Data;
using FactorioWebInterface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FactorioWebInterface.Pages.Admin
{
    public class BansModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManger;
        private readonly IFactorioServerManager _factorioServerManager;

        public BansModel(UserManager<ApplicationUser> userManger, IFactorioServerManager factorioServerManager)
        {
            _userManger = userManger;
            _factorioServerManager = factorioServerManager;
        }

        public List<Ban> Bans { get; private set; }

        public class InputModel
        {
            public string Admin { get; set; }
            public string Date { get; set; }
            public string Time { get; set; }
            public bool SynchronizeWithServers { get; set; } = true;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManger.GetUserAsync(User);

            if (user == null || user.Suspended)
            {
                HttpContext.Session.SetString("returnUrl", "bans");
                return RedirectToPage("signIn");
            }

            var now = DateTime.UtcNow;

            Input = new InputModel
            {
                Admin = user.UserName,
                Date = now.ToString("yyyy-MM-dd"),
                Time = now.ToString("HH:mm:ss"),
            };

            Bans = await _factorioServerManager.GetBansAsync();

            return Page();
        }
    }
}