// Hàm khởi tạo slider cho mỗi .slider-product
function initSliderForProductSlider(sliderProductElement) {
    let slider = sliderProductElement.querySelector('.list');
    let items = sliderProductElement.querySelectorAll('.list .item');
    let next = sliderProductElement.querySelector('.buttons .next');
    let prev = sliderProductElement.querySelector('.buttons .prev');

    let lengthItems = items.length - 1;
    let active = 0;
    let refreshInterval = setInterval(nextSlide, 3000);

    function nextSlide() {
        active = active + 1 <= lengthItems ? active + 1 : 0;
        reloadSlider();
    }

    function prevSlide() {
        active = active - 1 >= 0 ? active - 1 : lengthItems;
        reloadSlider();
    }

    function reloadSlider() {
        slider.style.left = -items[active].offsetLeft + 'px';

        clearInterval(refreshInterval);
        refreshInterval = setInterval(nextSlide, 3000);
    }

    next.onclick = nextSlide;
    prev.onclick = prevSlide;

    window.onresize = function(event) {
        reloadSlider();
    };

    // Khởi chạy slider
    reloadSlider();
}

// Lặp qua mỗi thẻ .slider-product và khởi tạo slider cho nó
document.querySelectorAll('.slider-product').forEach(initSliderForProductSlider);
