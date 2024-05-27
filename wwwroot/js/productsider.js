jQuery(document).ready(function ($) {
    $('.sidebar').on('click', '.sub-btn', function () {
        $(this).next('.sub-menu').stop(true, true).slideToggle(300);
        $(this).find('.dropdown').toggleClass('fa-bicycle fa-person-biking');
    });
});
