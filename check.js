var fs = require("fs");
var path = "C:/Users/gatchi/AppData/Local/User Name/maui-package-name-placeholder/Data/";

// Residents
var residents = [];
try {
  residents = JSON.parse(fs.readFileSync(path + "Residents.json", "utf8"));
} catch(e) { console.log("No Residents.json found"); }
console.log("Residents:", residents.length);
residents.forEach(function(r) {
  console.log("  ", r.Id, "|", r.ResidentName || r.Name);
});

// Resident-assigned meds
var meds = JSON.parse(fs.readFileSync(path + "Medications.json", "utf8"));
var empty = "00000000-0000-0000-0000-000000000000";
var assigned = meds.filter(function(m) { return m.ResidentId && m.ResidentId !== empty; });
console.log("\nResident-assigned meds:", assigned.length);
assigned.forEach(function(m) {
  console.log("  ", m.MedName, "|", m.ResidentName, "|", m.ResidentId);
});
