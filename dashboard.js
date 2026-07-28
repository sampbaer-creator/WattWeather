const $ = (selector, root = document) => root.querySelector(selector);
const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
const LOCATION_KEY = "wattweather.public.location.v2";
const page = document.body.dataset.page;
let selectedCity = loadLocation();
let cityResults = [];
let searchTimer;
let searchController;
let eiaData;

const sourceProfiles = {
  Alabama:["Natural gas","gas"],Alaska:["Natural gas","gas"],Arizona:["Natural gas","gas"],Arkansas:["Natural gas","gas"],
  California:["Natural gas","gas"],Colorado:["Wind","wind"],Connecticut:["Nuclear","nuclear"],Delaware:["Natural gas","gas"],
  Florida:["Natural gas","gas"],Georgia:["Natural gas","gas"],Hawaii:["Petroleum","oil"],Idaho:["Hydropower","water"],
  Illinois:["Nuclear","nuclear"],Indiana:["Coal","coal"],Iowa:["Wind","wind"],Kansas:["Wind","wind"],Kentucky:["Coal","coal"],
  Louisiana:["Natural gas","gas"],Maine:["Hydropower","water"],Maryland:["Nuclear","nuclear"],Massachusetts:["Natural gas","gas"],
  Michigan:["Natural gas","gas"],Minnesota:["Wind","wind"],Mississippi:["Natural gas","gas"],Missouri:["Coal","coal"],
  Montana:["Coal","coal"],Nebraska:["Coal","coal"],Nevada:["Natural gas","gas"],"New Hampshire":["Nuclear","nuclear"],
  "New Jersey":["Natural gas","gas"],"New Mexico":["Wind","wind"],"New York":["Natural gas","gas"],
  "North Carolina":["Natural gas","gas"],"North Dakota":["Coal","coal"],Ohio:["Natural gas","gas"],Oklahoma:["Wind","wind"],
  Oregon:["Hydropower","water"],Pennsylvania:["Natural gas","gas"],"Rhode Island":["Natural gas","gas"],
  "South Carolina":["Nuclear","nuclear"],"South Dakota":["Wind","wind"],Tennessee:["Nuclear","nuclear"],Texas:["Natural gas","gas"],
  Utah:["Coal","coal"],Vermont:["Hydropower","water"],Virginia:["Natural gas","gas"],Washington:["Hydropower","water"],
  "West Virginia":["Coal","coal"],Wisconsin:["Natural gas","gas"],Wyoming:["Coal","coal"],"District of Columbia":["Natural gas","gas"]
};
const sourceDetails = {
  water:{symbol:"≈",category:"Renewable",description:"Flowing water is the state’s typical leading electricity resource. Hydropower output can shift with snowpack, rainfall, and reservoir conditions."},
  wind:{symbol:"↻",category:"Renewable",description:"Wind is the state’s typical leading electricity resource. Output changes with weather, so the wider grid balances it with other sources."},
  sun:{symbol:"☀",category:"Renewable",description:"Sunlight is the state’s typical leading electricity resource. Solar output peaks during bright daytime hours and falls to zero overnight."},
  gas:{symbol:"♨",category:"Fossil",description:"Natural gas is the state’s typical leading electricity resource. Gas plants can respond quickly when weather pushes demand higher."},
  oil:{symbol:"●",category:"Fossil",description:"Petroleum is the state’s typical leading electricity resource. Island grids often face higher fuel transport costs and electricity prices."},
  nuclear:{symbol:"⚛",category:"Nuclear",description:"Nuclear is the state’s typical leading electricity resource. Plants usually provide steady output across weather conditions."},
  coal:{symbol:"◆",category:"Fossil",description:"Coal is the state’s typical leading electricity resource. It provides dispatchable power but has comparatively high carbon emissions."}
};

function loadLocation(){try{return JSON.parse(localStorage.getItem(LOCATION_KEY))}catch{return null}}
function saveLocation(city){localStorage.setItem(LOCATION_KEY,JSON.stringify(city))}
function cityLabel(city){return [city.name,city.admin1,city.country].filter(Boolean).join(", ")}
function setText(selector,value){const node=$(selector);if(node)node.textContent=value}
function weatherLabel(code){return({0:"Clear skies",1:"Mainly clear",2:"Partly cloudy",3:"Overcast",45:"Fog",48:"Rime fog",51:"Light drizzle",53:"Drizzle",55:"Heavy drizzle",61:"Rain",63:"Moderate rain",65:"Heavy rain",71:"Snow",80:"Rain showers",95:"Thunderstorm"}[code]||"Current conditions")}
function weatherIcon(code){if(code===0||code===1)return"☀";if(code===2)return"⛅";if(code===3||code===45||code===48)return"☁";if(code>=71&&code<80)return"❄";if(code>=51&&code<70||code>=80&&code<90)return"☂";if(code>=95)return"ϟ";return"◒"}
function solarVerdict(value,price=15){
  const adjusted=value+(price-15)*.025;
  if(adjusted>=5)return{score:Math.min(96,Math.round(72+adjusted*4)),label:"Strong solar signal",note:"Local sunlight makes rooftop solar well worth a closer quote."};
  if(adjusted>=3.5)return{score:Math.round(48+adjusted*5),label:"Worth exploring",note:"The solar resource looks promising; roof and utility details will decide the economics."};
  return{score:Math.max(28,Math.round(26+adjusted*6)),label:"Conditional fit",note:"Solar may still work, but roof exposure, incentives, and electricity price matter more here."};
}
function correlationStrength(value){const n=Math.abs(value);return n<.2?"very weak":n<.4?"weak":n<.6?"moderate":n<.8?"strong":"very strong"}
function pearson(rows){if(rows.length<3)return null;const mx=rows.reduce((s,r)=>s+r.x,0)/rows.length,my=rows.reduce((s,r)=>s+r.y,0)/rows.length;const numerator=rows.reduce((s,r)=>s+(r.x-mx)*(r.y-my),0);const denominator=Math.sqrt(rows.reduce((s,r)=>s+(r.x-mx)**2,0)*rows.reduce((s,r)=>s+(r.y-my)**2,0));return denominator?numerator/denominator:null}

async function findCities(term){
  if(term.trim().length<2){cityResults=[];return showSuggestions()}
  searchController?.abort();
  searchController=new AbortController();
  const response=await fetch(`https://geocoding-api.open-meteo.com/v1/search?name=${encodeURIComponent(term)}&count=8&language=en&format=json`,{signal:searchController.signal});
  if(!response.ok)throw Error("City search is temporarily unavailable.");
  cityResults=(await response.json()).results||[];
  showSuggestions();
}
function showSuggestions(){
  $$("[data-city-suggestions]").forEach(menu=>{
    menu.innerHTML=cityResults.map((city,index)=>`<button type="button" data-city-index="${index}"><b>${city.name}</b><span>${[city.admin1,city.country].filter(Boolean).join(", ")}</span></button>`).join("");
    menu.hidden=!cityResults.length;
  });
}
function setupLocationForms(){
  $$("[data-city-input]").forEach(input=>{
    if(selectedCity)input.value=cityLabel(selectedCity);
    input.addEventListener("input",event=>{clearTimeout(searchTimer);searchTimer=setTimeout(()=>findCities(event.target.value).catch(error=>{if(error.name!=="AbortError")showError(error)}),220)});
    input.addEventListener("keydown",event=>{
      const menu=event.target.closest("form").querySelector("[data-city-suggestions]");
      const buttons=[...menu.querySelectorAll("button")];if(!buttons.length)return;
      let active=buttons.findIndex(button=>button.classList.contains("active"));
      if(event.key==="ArrowDown"){event.preventDefault();active=(active+1)%buttons.length}
      else if(event.key==="ArrowUp"){event.preventDefault();active=(active-1+buttons.length)%buttons.length}
      else if(event.key==="Enter"&&active>=0){event.preventDefault();buttons[active].click();return}
      else if(event.key==="Escape"){menu.hidden=true;return}else return;
      buttons.forEach((button,index)=>button.classList.toggle("active",index===active));buttons[active].scrollIntoView({block:"nearest"});
    });
  });
  $$("[data-city-suggestions]").forEach(menu=>menu.addEventListener("mousedown",event=>{
    const button=event.target.closest("button");if(!button)return;
    event.preventDefault();selectedCity=cityResults[Number(button.dataset.cityIndex)];saveLocation(selectedCity);
    $$("[data-city-input]").forEach(input=>input.value=cityLabel(selectedCity));
    $$("[data-city-suggestions]").forEach(node=>node.hidden=true);
  }));
  $$("[data-location-form]").forEach(form=>form.addEventListener("submit",async event=>{
    event.preventDefault();
    const button=form.querySelector("button[type='submit'],button:not([type])");button.disabled=true;
    try{
      const input=form.querySelector("[data-city-input]");
      if(!selectedCity||cityLabel(selectedCity)!==input.value){await findCities(input.value);selectedCity=cityResults[0]}
      if(!selectedCity)throw Error("Choose a city from the suggestion list.");
      saveLocation(selectedCity);$$("[data-city-input]").forEach(node=>node.value=cityLabel(selectedCity));
      await loadDashboard();
    }catch(error){showError(error)}finally{button.disabled=false}
  }));
  document.addEventListener("click",event=>{if(!event.target.closest(".city-picker"))$$("[data-city-suggestions]").forEach(menu=>menu.hidden=true)});
}
function showError(error){
  $$("[data-form-status]").forEach(node=>{node.textContent=error.message||"Something went wrong while loading city data.";node.classList.add("error")});
}
function showStatus(message){
  $$("[data-form-status]").forEach(node=>{node.textContent=message;node.classList.remove("error")});
}
async function getEia(){
  if(!eiaData){const response=await fetch("data/eia-state-energy.json");if(!response.ok)throw Error("State electricity data is temporarily unavailable.");eiaData=await response.json()}
  return eiaData;
}
async function getWeather(city){
  const fields="temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m";
  const daily="temperature_2m_max,temperature_2m_min,shortwave_radiation_sum";
  const response=await fetch(`https://api.open-meteo.com/v1/forecast?latitude=${city.latitude}&longitude=${city.longitude}&current=${fields}&daily=${daily}&temperature_unit=fahrenheit&wind_speed_unit=mph&timezone=auto`);
  if(!response.ok)throw Error("Local weather is temporarily unavailable.");
  return response.json();
}
async function loadDashboard(){
  if(!selectedCity)return;
  if(page==="incentives"){
    renderIncentives();
    showStatus(`Showing ${selectedCity.admin1 || selectedCity.name}. Verify each program before adding it to the estimate.`);
    return;
  }
  showStatus(`Loading weather and electricity data for ${cityLabel(selectedCity)}…`);
  const [weather,eia]=await Promise.all([getWeather(selectedCity),getEia()]);
  const state=eia.states?.[selectedCity.admin1];
  const solar=weather.daily.shortwave_radiation_sum.map(value=>value/3.6);
  if(page==="home")renderHome(weather,state,solar);
  if(page==="solar")renderSolar(weather,state,solar);
  if(page==="impact")await renderImpact(weather,state);
  if(page==="power")renderPower(state);
  showStatus(`Showing ${cityLabel(selectedCity)}. Data updates when you change cities.`);
}
function renderIncentives(){
  const state=selectedCity?.admin1;
  if(!state)return;
  setText("#incentive-state-title",`Search programs available in ${state}.`);
  const link=$("#dsire-link");
  if(link)link.setAttribute("aria-label",`Search DSIRE for solar programs in ${state}`);
}
function setupDiscountCalculator(){
  const inputs=["#quote-cost","#state-rebate","#utility-rebate"].map(selector=>$(selector)).filter(Boolean);
  if(!inputs.length)return;
  const money=new Intl.NumberFormat("en-US",{style:"currency",currency:"USD",maximumFractionDigits:0});
  const update=()=>{
    const [quote=0,stateRebate=0,utilityRebate=0]=inputs.map(input=>Math.max(0,Number(input.value)||0));
    const discount=Math.min(quote,stateRebate+utilityRebate);
    const net=Math.max(0,quote-discount);
    const percent=quote?Math.round(discount/quote*100):0;
    setText("#net-cost",money.format(net));
    setText("#discount-total",money.format(discount));
    setText("#discount-percent",`${percent}%`);
  };
  inputs.forEach(input=>input.addEventListener("input",update));
  update();
}
function renderHome(weather,state,solar){
  const current=weather.current;
  setText("#hero-city",selectedCity.name.toUpperCase());setText("#hero-temp",`${Math.round(current.temperature_2m)}°`);
  setText("#hero-icon",weatherIcon(current.weather_code));setText("#hero-condition",weatherLabel(current.weather_code));
  const verdict=solarVerdict(solar.reduce((a,b)=>a+b,0)/solar.length,state?.residentialPriceCents);
  setText("#hero-solar",`${verdict.score}/100`);setText("#hero-solar-note",verdict.label);
  const bar=$("#hero-solar-bar");if(bar)bar.style.width=`${verdict.score}%`;
  setText("#snapshot-title",`${selectedCity.name} at a glance.`);
  setText("#snapshot-weather",`${Math.round(current.temperature_2m)}°F`);
  setText("#snapshot-weather-note",`${weatherLabel(current.weather_code)} · feels like ${Math.round(current.apparent_temperature)}°`);
  setText("#snapshot-solar",`${solar[0].toFixed(1)} kWh/m²`);
  setText("#snapshot-usage",state?`${Math.round(state.averageMonthlyKwh).toLocaleString()} kWh/mo`:"Unavailable");
  setText("#snapshot-price",state?`${state.residentialPriceCents.toFixed(2)}¢/kWh`:"Unavailable");
  if(state){setText("#snapshot-usage-note",`${selectedCity.admin1} home average · EIA ${state.period}`);setText("#snapshot-price-note",`${selectedCity.admin1} residential rate · EIA ${state.period}`)}
}
function renderSolar(weather,state,solar){
  const average=solar.reduce((sum,value)=>sum+value,0)/solar.length;
  const price=state?.residentialPriceCents||15;
  const verdict=solarVerdict(average,price);
  const annualOutput=6*average*365*.8;
  setText("#solar-place-heading",`${selectedCity.name}?`);setText("#solar-score",`${verdict.score}/100`);setText("#solar-verdict",verdict.label);setText("#solar-verdict-note",verdict.note);
  setText("#solar-today",solar[0].toFixed(1));setText("#solar-average",average.toFixed(1));setText("#solar-output",`${Math.round(annualOutput).toLocaleString()} kWh`);
  setText("#solar-savings",`$${Math.round(annualOutput*price/100).toLocaleString()}`);setText("#solar-rate-note",state?`at ${price.toFixed(2)}¢/kWh`:"using a 15¢/kWh fallback");
  setText("#solar-answer",verdict.label+".");setText("#solar-explanation",verdict.note);
  const marker=$("#score-marker");if(marker)marker.style.left=`${verdict.score}%`;
  const max=Math.max(...solar),days=weather.daily.time.map(date=>new Date(`${date}T12:00:00`).toLocaleDateString(undefined,{weekday:"short"}));
  const bars=$("#solar-bars");if(bars)bars.innerHTML=solar.map((value,index)=>`<div><b>${value.toFixed(1)}</b><i style="height:${Math.max(4,value/max*100)}%"></i><span>${days[index]}</span></div>`).join("");
}
function renderImpactChart(weather){
  const averages=weather.daily.temperature_2m_max.map((high,index)=>(high+weather.daily.temperature_2m_min[index])/2);
  const min=Math.min(...averages,40),max=Math.max(...averages,90),range=max-min||1;
  const days=weather.daily.time.map(date=>new Date(`${date}T12:00:00`).toLocaleDateString(undefined,{weekday:"short"}));
  const chart=$("#temperature-impact-chart");
  if(chart)chart.innerHTML=averages.map((value,index)=>`<div class="impact-day"><b>${Math.round(value)}°</b><i class="${value>65?"hot":""}" style="height:${Math.max(8,(value-min)/range*88+8)}%"></i><span>${days[index]}</span></div>`).join("");
}
async function renderImpact(weather,state){
  const current=weather.current,feels=current.apparent_temperature;
  const heating=Math.max(0,65-feels),cooling=Math.max(0,feels-65);
  setText("#impact-city",cityLabel(selectedCity).toUpperCase());setText("#impact-temp",`${Math.round(current.temperature_2m)}°`);setText("#impact-icon",weatherIcon(current.weather_code));setText("#impact-condition",weatherLabel(current.weather_code));
  setText("#impact-feels",`${Math.round(feels)}°F`);setText("#heating-pressure",heating?`${Math.round(heating)}°`:"Low");setText("#cooling-pressure",cooling?`${Math.round(cooling)}°`:"Low");
  setText("#impact-state-usage",state?`${Math.round(state.averageMonthlyKwh).toLocaleString()} kWh`:"Unavailable");
  if(state)setText("#impact-state-note",`${selectedCity.admin1} home average · EIA ${state.period}`);
  const pressure=heating>12?"Cold weather is likely adding meaningful heating demand.":cooling>12?"Hot weather is likely adding meaningful cooling demand.":"Current weather is close to the 65°F comfort baseline.";
  setText("#impact-answer",pressure);setText("#impact-answer-note",`In ${selectedCity.name}, it currently feels like ${Math.round(feels)}°F. Building efficiency and heating fuel determine how strongly that appears on an electric bill.`);
  renderImpactChart(weather);
  if(!state){setText("#impact-correlation-note","State electricity history is available for U.S. cities.");return}
  try{
    setText("#impact-correlation","…");setText("#impact-correlation-note","Comparing ten years of city temperature with state household use…");
    const history=state.history.slice(-10),start=`${history[0].period}-01-01`,end=`${history.at(-1).period}-12-31`;
    const response=await fetch(`https://archive-api.open-meteo.com/v1/archive?latitude=${selectedCity.latitude}&longitude=${selectedCity.longitude}&start_date=${start}&end_date=${end}&daily=temperature_2m_mean&temperature_unit=fahrenheit&timezone=auto`);
    if(!response.ok)throw Error();
    const archive=await response.json(),groups={};
    archive.daily.time.forEach((date,index)=>{const year=date.slice(0,4),value=archive.daily.temperature_2m_mean[index];if(Number.isFinite(value))(groups[year]??=[]).push(value)});
    const annual=Object.fromEntries(Object.entries(groups).map(([year,values])=>[year,values.reduce((a,b)=>a+b,0)/values.length]));
    const joined=history.filter(row=>Number.isFinite(annual[row.period])).map(row=>({x:annual[row.period],y:row.averageMonthlyKwh}));
    const value=pearson(joined);setText("#impact-correlation",value===null?"—":value.toFixed(2));
    setText("#impact-correlation-note",value===null?"Not enough matched years.":`${joined.length} matched years show a ${correlationStrength(value)} association between ${selectedCity.name} temperature and ${selectedCity.admin1} residential use.`);
  }catch{setText("#impact-correlation","—");setText("#impact-correlation-note","The longer-term comparison is temporarily unavailable. Current weather pressure is still shown above.")}
}
function renderPower(state){
  const profile=sourceProfiles[selectedCity.admin1]||["Regional grid mix","gas"],name=profile[0],key=profile[1],details=sourceDetails[key];
  setText("#source-symbol",details.symbol);setText("#source-name",name);setText("#source-icon",details.symbol);
  setText("#source-state-label",selectedCity.admin1.toUpperCase());setText("#source-answer-title",`${name} typically leads ${selectedCity.admin1}’s electricity mix.`);
  setText("#source-description",details.description);setText("#power-leading",name);setText("#power-category",details.category);
  setText("#power-price",state?`${state.residentialPriceCents.toFixed(2)}¢/kWh`:"Unavailable");setText("#power-usage",state?`${Math.round(state.averageMonthlyKwh).toLocaleString()} kWh/mo`:"Unavailable");
  if(state){setText("#power-price-note",`${selectedCity.admin1} home rate · EIA ${state.period}`);setText("#power-usage-note",`${selectedCity.admin1} household average · EIA ${state.period}`)}
  $$(".source-card").forEach(card=>card.classList.toggle("active",card.classList.contains(key)));
}

setupLocationForms();
setupDiscountCalculator();
if(selectedCity)loadDashboard().catch(showError);
