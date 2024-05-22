const ul = document.querySelector('.page-numbers');
let allPages = 15;
function elem(allPages, page) {
    let li = '';
    let beforePages = page - 1;
    let afterPages = page + 1;
    let liActive;
    if (page > 1) {
        li +='<li class="page-number" onclick="elem(allPages , $(page-1))" ><a href="#"><i class="fa-solid fa-angle-left"></i></a></li>'; 
    }
    for(let pageLength = beforePages; pageLength <= afterPages ; pageLength++){
        if(page == pageLength){
            liActive = 'active';
        }else{
            liActive='';
        }

        li+='<li class="page-number" onclick="elem(allPages , $(page+1)) ><span aria-current="page" class="page-number ${liActive}">${pageLength}</span></li>';
        
    }
    if(page < allPages){
        li +='<li class="page-number"><a href="#"><i class="fa-solid fa-angle-right"></i></a></li>';
    }
    ul.innerHTML=li;
}
elem(15,2)
