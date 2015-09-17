I have added 

@section Scripts {
    @Scripts.Render("~/bundles/jqueryval")
    @Styles.Render("~/Content/css")
    <link href="@Url.Content("~/Content/Home.css")" rel="stylesheet" type="text/css" />
}

to each page and modified some of the controllers, such as TextBox and other using Bootstrap.
I have decided do not put each and every view to this folder.