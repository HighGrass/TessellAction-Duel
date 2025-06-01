const mongoose = require('mongoose');
const bcrypt = require('bcryptjs');
const User = require('../models/User');
require('dotenv').config();

async function createUser() {
  try {
    await mongoose.connect(process.env.MONGODB_URI);

    const password = 'senha123';
    const passwordHash = await bcrypt.hash(password, 10);

    const user = new User({
      username: 'Martim',
      passwordHash: passwordHash,
      globalScore: 0
    });

    await user.save();
    console.log('Usuário criado com sucesso');
    mongoose.disconnect();
  } catch (error) {
    console.error('Erro ao criar usuário:', error);
  }
}

createUser();