const clamp=(n,a,b)=>Math.max(a,Math.min(b,n));
const seeded=(()=>{let s=20260727;return()=>((s=Math.imul(1664525,s)+1013904223|0)>>>0)/4294967296})();
const data=Array.from({length:730},(_,i)=>{
  const date=new Date();date.setDate(date.getDate()-(729-i));
  const doy=Math.floor((date-new Date(date.getFullYear(),0,0))/86400000);
  const temp=54+28*Math.sin(2*Math.PI*(doy-105)/365.25)+(seeded()-.5)*12;
  let usage=16+Math.max(0,65-temp)*.38+Math.max(0,temp-68)*.52+(date.getDay()%6===0?2.2:0)+(seeded()-.5)*4.2;
  if(i%97===0)usage+=16; usage=Math.max(4,usage);
  const rate=.135+(date.getFullYear()-dataStartYear())*.006;
  return{date,temp,usage,cost:usage*rate,rate};
});
function dataStartYear(){const d=new Date();d.setDate(d.getDate()-729);return d.getFullYear()}
const usage=data.map(d=>d.usage),avg=usage.reduce((a,b)=>a+b,0)/usage.length,total=usage.reduce((a,b)=>a+b,0);
const sorted=[...usage].sort((a,b)=>a-b),median=(sorted[364]+sorted[365])/2;
const sd=Math.sqrt(usage.reduce((s,n)=>s+(n-avg)**2,0)/(usage.length-1));
const meanT=data.reduce((s,d)=>s+d.temp,0)/data.length;
const covariance=data.reduce((s,d)=>s+(d.temp-meanT)*(d.usage-avg),0);
const corr=covariance/Math.sqrt(data.reduce((s,d)=>s+(d.temp-meanT)**2,0)*usage.reduce((s,n)=>s+(n-avg)**2,0));
const q1=sorted[Math.floor(sorted.length*.25)],q3=sorted[Math.floor(sorted.length*.75)],high=q3+1.5*(q3-q1),anomalies=usage.filter(n=>n>high).length;
const money=new Intl.NumberFormat("en-US",{style:"currency",currency:"USD"});
document.querySelector("#hero-average").textContent=`${avg.toFixed(1)} kWh`;
document.querySelector("#stat-days").textContent=data.length.toLocaleString();
document.querySelector("#stat-kwh").textContent=Math.round(total).toLocaleString();
document.querySelector("#stat-correlation").textContent=corr.toFixed(2);
document.querySelector("#stat-anomalies").textContent=anomalies;
document.querySelector("#mini-bars").innerHTML=data.slice(-18).map(d=>`<i style="height:${clamp(d.usage/55*100,10,100)}%"></i>`).join("");
const kpis=[["Total usage",Math.round(total).toLocaleString(),"kWh"],["Daily average",avg.toFixed(1),"kWh / day"],["Total cost",money.format(data.reduce((s,d)=>s+d.cost,0)),"observed period"],["Monthly estimate",money.format(avg*30*.145),(avg*30).toFixed(0)+" kWh"],["Temp correlation",corr.toFixed(2),"Pearson r"],["Unusual days",anomalies,"IQR method"]];
document.querySelector("#kpis").innerHTML=kpis.map(k=>`<article><span>${k[0]}</span><strong>${k[1]}</strong><small>${k[2]}</small></article>`).join("");
document.querySelector("#usage-chart").innerHTML=data.slice(-90).map(d=>`<i title="${d.date.toLocaleDateString()}: ${d.usage.toFixed(1)} kWh" style="height:${clamp(d.usage/Math.max(...usage)*100,3,100)}%"></i>`).join("");
document.querySelector("#distribution").innerHTML=[["Median",median.toFixed(1)+" kWh"],["Minimum",Math.min(...usage).toFixed(1)+" kWh"],["Maximum",Math.max(...usage).toFixed(1)+" kWh"],["Std. deviation",sd.toFixed(1)+" kWh"]].map(x=>`<span>${x[0]} <b>${x[1]}</b></span>`).join("");
const hdd=data.reduce((s,d)=>s+Math.max(0,65-d.temp),0),cdd=data.reduce((s,d)=>s+Math.max(0,d.temp-65),0);
document.querySelector("#hdd").textContent=Math.round(hdd).toLocaleString();document.querySelector("#cdd").textContent=`${Math.round(cdd).toLocaleString()} cooling degree days at a 65°F base.`;
document.querySelector("#energy-rows").innerHTML=data.slice(-100).reverse().map(d=>`<tr><td>${d.date.toLocaleDateString(undefined,{month:"short",day:"numeric",year:"numeric"})}</td><td>${d.temp.toFixed(0)}° · ${d.temp<32?"Snow possible":d.temp>82?"Clear":"Partly cloudy"}</td><td><strong>${d.usage.toFixed(1)} kWh</strong></td><td>${money.format(d.cost)}</td><td>${money.format(d.rate)}</td></tr>`).join("");
const weatherCode=c=>({0:"Clear skies",1:"Mainly clear",2:"Partly cloudy",3:"Overcast",45:"Fog",51:"Light drizzle",61:"Rain",71:"Snow",80:"Rain showers",95:"Thunderstorm"}[c]||"Current conditions");
document.querySelector("#weather-form").addEventListener("submit",async e=>{
 e.preventDefault();const city=document.querySelector("#city").value.trim(),error=document.querySelector("#weather-error");error.hidden=true;
 try{
  const geo=await fetch(`https://geocoding-api.open-meteo.com/v1/search?name=${encodeURIComponent(city)}&count=1&language=en&format=json`).then(r=>r.json());
  if(!geo.results?.length)throw new Error("Location not found. Try a city and state or country.");
  const p=geo.results[0],w=await fetch(`https://api.open-meteo.com/v1/forecast?latitude=${p.latitude}&longitude=${p.longitude}&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m&daily=temperature_2m_max,temperature_2m_min&temperature_unit=fahrenheit&wind_speed_unit=mph&timezone=auto`).then(r=>r.json());
  document.querySelector("#weather-place").textContent=`${p.name.toUpperCase()}, ${p.admin1||p.country}`;
  document.querySelector("#weather-temp").textContent=`${Math.round(w.current.temperature_2m)}°`;
  document.querySelector("#weather-description").textContent=weatherCode(w.current.weather_code);
  document.querySelector("#weather-feels").textContent=`${Math.round(w.current.apparent_temperature)}°`;
  document.querySelector("#weather-humidity").textContent=`${w.current.relative_humidity_2m}%`;
  document.querySelector("#weather-wind").textContent=`${w.current.wind_speed_10m.toFixed(1)} mph`;
  document.querySelector("#weather-highlow").textContent=`${Math.round(w.daily.temperature_2m_max[0])}° / ${Math.round(w.daily.temperature_2m_min[0])}°`;
 }catch(ex){error.textContent=ex.message;error.hidden=false}
});
