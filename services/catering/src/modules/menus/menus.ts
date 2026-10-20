// The catering provider's own data. It is not part of SmartRail's database: SmartRail reads it over HTTP and never stores it.

export interface Localised {
  en: string;
  si: string;
}

export interface MenuItem {
  id: string;
  name: Localised;
  price: number;
}

export interface MenuCategory {
  id: "meals" | "snacks" | "drinks";
  name: Localised;
  items: MenuItem[];
}

export interface StationMenu {
  stationCode: string;
  currency: "LKR";
  // Ordering and payment stay with the provider: the passenger calls this number.
  orderPhone: string;
  categories: MenuCategory[];
}

const categories = (meals: MenuItem[], snacks: MenuItem[], drinks: MenuItem[]): MenuCategory[] => [
  { id: "meals", name: { en: "Meals", si: "ආහාර වේල්" }, items: meals },
  { id: "snacks", name: { en: "Snacks", si: "කෙටි ආහාර" }, items: snacks },
  { id: "drinks", name: { en: "Drinks", si: "පානීය" }, items: drinks },
];

const item = (id: string, en: string, si: string, price: number): MenuItem => ({ id, name: { en, si }, price });

const roll = item("roll", "Egg and vegetable roll", "බිත්තර සහ එළවළු රොටි", 180);
const rice = item("rice", "Chicken fried rice", "චිකන් ෆ්‍රයිඩ් රයිස්", 650);
const kottu = item("kottu", "Vegetable kottu", "එළවළු කොත්තු", 520);
const cutlet = item("cutlet", "Fish cutlet", "මාළු කට්ලට්", 90);
const samosa = item("samosa", "Vegetable samosa", "එළවළු සමෝසා", 70);
const tea = item("tea", "Tea", "තේ", 60);
const water = item("water", "Bottled water", "බෝතල් වතුර", 80);

// Demo data for a fictional network: only some stations have a kitchen.
export const menus: Record<string, StationMenu> = {
  OAK: {
    stationCode: "OAK",
    currency: "LKR",
    orderPhone: "+94 11 555 0101",
    categories: categories([roll, rice, kottu], [cutlet, samosa], [tea, water]),
  },
  CEN: {
    stationCode: "CEN",
    currency: "LKR",
    orderPhone: "+94 11 555 0102",
    categories: categories([rice, kottu], [cutlet, samosa], [tea, water]),
  },
  RIV: {
    stationCode: "RIV",
    currency: "LKR",
    orderPhone: "+94 11 555 0103",
    categories: categories([roll, kottu], [samosa], [tea, water]),
  },
  AIR: {
    stationCode: "AIR",
    currency: "LKR",
    orderPhone: "+94 11 555 0104",
    categories: categories([rice, kottu, roll], [cutlet, samosa], [tea, water]),
  },
};
