        //
        //GET: /Account/ViewAccounts
        public ActionResult ViewAccounts()
        {
            ViewAccounts returnView = new ViewAccounts()
            {
                ifbEntity = GetAllAccounts().OrderBy(x=>x.FirstName)
            };

            return View(returnView);
        }

        //
        //POST: /Account/ViewAccounts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ViewAccounts(ViewAccounts model, string id)
        {
            var findUser = db.AspNetUsers.Find(id);
            if (!findUser.UserName.Equals("ifb299books")) 
            {
                var delete = db.AspNetUsers.Remove(findUser);
                db.SaveChanges();
            }

            model = new ViewAccounts
            {
                ifbEntity = GetAllAccounts().OrderBy(x=>x.FirstName)    
            };

            return View(model);
        }
        
        //
        // POST: /Account/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,FirstName,LastName,Phone,Email")]ViewAccounts model)
        {
            var editedUser = db.AspNetUsers.Find(model.Id);
            editedUser.Email = model.Email;
            editedUser.PhoneNumber = model.Phone;
            editedUser.LastName = model.LastName;
            editedUser.FirstName = model.FirstName;
            editedUser.Id = model.Id;

            if (ModelState.IsValid)
            {
                db.Entry(editedUser).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("/ViewAccounts");
            }
            else 
            {
                return HttpNotFound();
            }

        }

        public IEnumerable<ViewAccounts> GetAllAccounts()
        {
            return db.AspNetUsers.Select(x => new ViewAccounts 
        {
                Phone = x.PhoneNumber.ToString(), 
                Email = x.Email, Id = x.Id, 
                FirstName = x.FirstName,
                LastName = x.LastName

            }).AsEnumerable();
        }
        