import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import { 
  Button, 
  Rate, 
  Field, 
  CellGroup, 
  Checkbox, 
  CheckboxGroup, 
  NavBar, 
  Toast,
  Divider
} from 'vant'

import 'vant/lib/index.css'

const app = createApp(App)

app.use(router)
app.use(Button)
app.use(Rate)
app.use(Field)
app.use(CellGroup)
app.use(Checkbox)
app.use(CheckboxGroup)
app.use(NavBar)
app.use(Toast)
app.use(Divider)

app.mount('#app')

