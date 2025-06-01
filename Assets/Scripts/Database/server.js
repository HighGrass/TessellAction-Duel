const express = require('express');
const mongoose = require('mongoose');
const dotenv = require('dotenv');
const helmet = require('helmet');
const cors = require('cors');
const rateLimit = require('express-rate-limit');
const authRoutes = require('./routes/authRoutes');

dotenv.config();

const app = express();

// Middlewares de Segurança
app.use(cors());
app.use(helmet());
app.use(express.json());
app.use(rateLimit({ windowMs: 15 * 60 * 1000, max: 100 }));

// Conectar ao MongoDB
mongoose.connect(process.env.MONGODB_URI)
    .then(() => console.log('Conectado ao MongoDB Atlas'))
    .catch(err => console.error('Erro de conexão:', err));

// Rotas
app.use('/api/auth', authRoutes);

app.listen(process.env.PORT, () => {
    console.log(`Backend rodando em http://localhost:${process.env.PORT}`);
});