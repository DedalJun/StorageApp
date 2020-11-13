using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StorageApp
{
    public partial class Form1 : Form
    {
        StorageContext db;
        public Form1()
        {
            db = new StorageContext();
            db.Currencies.Load();
            db.Warehouses.Load();
            db.Products.Load();
            db.Categories.Load();
            
            InitializeComponent();
            updateCurrencies();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            button4.Visible = true;
            button1.Visible = false;
            button2.Visible = false;
            button3.Visible = false;
            dataGridView1.DataSource = db.Currencies.Local.ToBindingList();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            button4.Visible = false;
            button1.Visible = true;
            button2.Visible = true;
            button3.Visible = true;
            dataGridView1.DataSource = db.Warehouses.Local.ToBindingList();
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            button4.Visible = false;
            button1.Visible = true;
            button2.Visible = true;
            button3.Visible = true;
            dataGridView1.DataSource = db.Categories.Local.ToBindingList();
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            button4.Visible = false;
            button1.Visible = true;
            button2.Visible = true;
            button3.Visible = true;
            dataGridView1.DataSource = db.Products.Local.ToBindingList();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            addRecord();
        }

        public void addRecord()
        {
            if (radioButton2.Checked)
            {
                WarehouseForm WForm = new WarehouseForm();
                DialogResult result = WForm.ShowDialog(this);

                if (result == DialogResult.Cancel) return;

                Warehouse warehouse = new Warehouse();
                warehouse.Id = db.Warehouses.Count();
                warehouse.name = WForm.textBox1.Text;
                warehouse.adress = WForm.textBox2.Text;

                db.Warehouses.Add(warehouse);
                db.SaveChanges();

                dataGridView1.Refresh();
                MessageBox.Show("New record added");
            }

            else if (radioButton3.Checked)
            {
                CategoryForm CForm = new CategoryForm();
                DialogResult result = CForm.ShowDialog(this);

                if (result == DialogResult.Cancel) return;

                Category category = new Category();
                category.Id = db.Categories.Count();
                category.name = CForm.textBox1.Text;

                db.Categories.Add(category);
                db.SaveChanges();

                dataGridView1.Refresh();
                MessageBox.Show("New record added");
            }

            else if (radioButton4.Checked)
            {
                ProductForm PForm = new ProductForm();
                
                List<Currency> currency = new List<Currency>(db.Currencies);
                PForm.comboBox1.DataSource = currency;
                PForm.comboBox1.DisplayMember = "name";
                PForm.comboBox1.ValueMember = "name";

                List<Category> categories = new List<Category>(db.Categories);
                PForm.comboBox2.DataSource = categories;
                PForm.comboBox2.DisplayMember = "name";
                PForm.comboBox2.ValueMember = "name";
                DialogResult result = PForm.ShowDialog(this);

                if (result == DialogResult.Cancel) return;

                Product product = new Product();
                product.Id = db.Products.Count();
                product.name = PForm.textBox1.Text;
                product.basePrice = calculateDefaultPrice(Convert.ToDouble(PForm.textBox3.Text), (string)PForm.comboBox1.SelectedValue);
                product.price = Convert.ToDouble(PForm.textBox3.Text);
                product.productCurrency = (string)PForm.comboBox1.SelectedValue;
                product.Categoryname = (string)PForm.comboBox2.SelectedValue;
                Random rnd = new Random();
                product.barcode = rnd.Next(10000000, 99999999);

                db.Products.Add(product);
                db.SaveChanges();

                dataGridView1.Refresh();
                MessageBox.Show("New record added");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            updateCurrencies();
        }

        void updateCurrencies()
        {
            String data = DateTime.Now.ToShortDateString();
            Double e = 28;

            var json = "";

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create("https://bank.gov.ua/NBU_Exchange/exchange?json");

            HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

            StreamReader myStreamReader = new StreamReader(myHttpWebResponse.GetResponseStream());

            json = myStreamReader.ReadToEnd();

            var ex = JsonConvert.DeserializeObject<List<Exchanges>>(json).Where(x => x.CurrencyCodeL == "EUR" || x.CurrencyCodeL == "USD");


            if (db.Currencies.Count() == 0)
            {
                foreach (Exchanges x in ex)
                {
                    Currency currency = new Currency();
                    currency.Id = x.CurrencyCode;
                    currency.name = x.CurrencyCodeL;
                    currency.Exchange = x.Amount / x.Units;
                    currency.updateTime = Convert.ToDateTime(data);
                    if (x.CurrencyCodeL == "USD") e = x.Amount / x.Units;
                    db.Currencies.Add(currency);
                    db.SaveChanges();
                }
                Currency currencyy = new Currency();
                currencyy.Id = 980;
                currencyy.name = "UAH";
                currencyy.Exchange = 1;
                currencyy.updateTime = Convert.ToDateTime(data);

                db.Currencies.Add(currencyy);
                db.SaveChanges();
            }
            else
            {
                foreach (Exchanges x in ex)
                {
                    if (x.CurrencyCodeL == "USD") e = x.Amount / x.Units;
                    foreach (Currency currency in db.Currencies)
                    {
                        if (currency.name == x.CurrencyCodeL)
                            currency.Exchange = x.Amount / x.Units;
                        currency.updateTime = Convert.ToDateTime(data);
                        if (currency.name == "UAH") currency.Exchange = 1;
                    }
                    db.SaveChanges();
                }

            }
            changeDefaultCurrency(e);
            dataGridView1.Refresh();
        }

        void changeDefaultCurrency(Double e)
        {
            int a;
            foreach (Currency currency in db.Currencies)
            {
                a = Convert.ToInt32((currency.Exchange / e) * 10000);
                currency.Exchange = (double)a / 10000;
            }
            db.SaveChanges();
            dataGridView1.Refresh();
        }

        double calculateDefaultPrice(double price, String curr)
        {
            double amount = 1;

            foreach (Currency currency in db.Currencies)
            {
                if (currency.name == curr) amount = currency.Exchange;
            }

            return price * amount;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            editRecord();
        }

        void editRecord()
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                int index = dataGridView1.SelectedRows[0].Index;
                int id = 0;
                bool converted = Int32.TryParse(dataGridView1[0, index].Value.ToString(), out id);
                if (converted == false) return;
                if (radioButton2.Checked)
                {

                    Warehouse warehouse = db.Warehouses.Find(id);
                    WarehouseForm WForm = new WarehouseForm();
                    WForm.textBox1.Text = warehouse.name;
                    WForm.textBox2.Text = warehouse.adress;
                    DialogResult result = WForm.ShowDialog(this);

                    if (result == DialogResult.Cancel) return;

                    warehouse.name = WForm.textBox1.Text;
                    warehouse.adress = WForm.textBox2.Text;
                    db.SaveChanges();

                    dataGridView1.Refresh();
                    MessageBox.Show("Record updated");
                }


                else if (radioButton3.Checked)
                {
                    Category category = db.Categories.Find(id);
                    CategoryForm CForm = new CategoryForm();
                    CForm.textBox1.Text = category.name;
                    DialogResult result = CForm.ShowDialog(this);

                    if (result == DialogResult.Cancel) return;

                    category.name = CForm.textBox1.Text;

                    db.SaveChanges();

                    dataGridView1.Refresh();
                    MessageBox.Show("Record updated");
                }

                else if (radioButton4.Checked)
                {
                    ProductForm PForm = new ProductForm();

                    List<Currency> currency = new List<Currency>(db.Currencies);
                    PForm.comboBox1.DataSource = currency;
                    PForm.comboBox1.DisplayMember = "name";
                    PForm.comboBox1.ValueMember = "name";

                    List<Category> categories = new List<Category>(db.Categories);
                    PForm.comboBox2.DataSource = categories;
                    PForm.comboBox2.DisplayMember = "name";
                    PForm.comboBox2.ValueMember = "name";
                    

                    Product product = db.Products.Find(id);
                    PForm.textBox1.Text = product.name;
                    PForm.textBox3.Text = Convert.ToString(product.price);
                    PForm.comboBox1.SelectedValue = Convert.ToString(product.productCurrency);
                    PForm.comboBox2.SelectedValue = Convert.ToString(product.Categoryname);

                    DialogResult result = PForm.ShowDialog(this);
                    if (result == DialogResult.Cancel) return;

                    product.name = PForm.textBox1.Text;
                    product.basePrice = calculateDefaultPrice(Convert.ToDouble(PForm.textBox3.Text), (string)PForm.comboBox1.SelectedValue);
                    product.price = Convert.ToDouble(PForm.textBox3.Text);
                    product.productCurrency = (string)PForm.comboBox1.SelectedValue;
                    product.Categoryname = (string)PForm.comboBox2.SelectedValue;

                    db.SaveChanges();

                    dataGridView1.Refresh();
                    MessageBox.Show("Record updated");
                }
            }
        }

        void deleteRecord()
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                int index = dataGridView1.SelectedRows[0].Index;
                int id = 0;
                bool converted = Int32.TryParse(dataGridView1[0, index].Value.ToString(), out id);
                if (converted == false) return;

                if(radioButton2.Checked)
                {
                    Warehouse warehouse = db.Warehouses.Find(id);
                    db.Warehouses.Remove(warehouse);
                    db.SaveChanges();
                    dataGridView1.Refresh();
                    MessageBox.Show("Record removed");
                }

                else if(radioButton3.Checked)
                {
                    Category category = db.Categories.Find(id);
                    db.Categories.Remove(category);
                    db.SaveChanges();
                    dataGridView1.Refresh();
                    MessageBox.Show("Record removed");
                }

                else if(radioButton4.Checked)
                {
                    Product product = db.Products.Find(id);
                    db.Products.Remove(product);
                    db.SaveChanges();
                    dataGridView1.Refresh();
                    MessageBox.Show("Record removed");
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            deleteRecord();
        }
    }
}
