window.addEventListener('scroll', function() {
    var contactIcons = document.getElementById('contact-icons');
    var scrollPosition = window.scrollY || document.documentElement.scrollTop;

    if (scrollPosition > 100) { // Thay số 500 bằng khoảng cách bạn muốn
        contactIcons.classList.add('visible');
    } else {
        contactIcons.classList.remove('visible');
    }
});
